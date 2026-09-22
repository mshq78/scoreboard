<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { getEvaluatorBootstrap, syncEvaluator } from '@/api/evaluatorApi'
import { getApiErrorMessage } from '@/api/http'
import {
    clearLocalScore,
    getLocalEvaluation,
    makeCommentKey,
    makeScoreKey,
    markLocalSynced,
    saveBootstrapToLocal,
    saveLocalComment,
    saveLocalScore,
} from '@/offline/evaluatorDb'
import { detectTeamColorVisual } from '@/utils/teamColor'
import type {
    EvaluatorSyncRequest,
    LocalEvaluationState,
} from '@/types/evaluator'

const props = defineProps<{
    token: string
}>()

const state = ref<LocalEvaluationState | null>(null)
const isLoading = ref(false)
const isSyncing = ref(false)
const isOnline = ref(navigator.onLine)

const errorMessage = ref('')
const successMessage = ref('')
const passiveMessage = ref('')
const passiveTone = ref<'info' | 'success' | 'warning' | 'danger'>('info')
const validationMessage = ref('')
const isFinalConfirmModalOpen = ref(false)
const syncErrorModal = ref<{ title: string; message: string } | null>(null)

const activeEventId = ref<string | null>(null)
const selectedTeamIds = ref<string[]>([])

const bootstrap = computed(() => state.value?.bootstrap ?? null)

const isReadOnly = computed(() => {
    return state.value?.isFinalized || state.value?.bootstrap.isReadOnly || false
})

const expectedScoreCount = computed(() => {
    if (!bootstrap.value) {
        return 0
    }

    const teamCount = bootstrap.value.teams.length
    const indicatorCount = bootstrap.value.events.reduce(
        (sum, event) => sum + event.indicators.length,
        0,
    )

    return teamCount * indicatorCount
})

const submittedScoreCount = computed(() => {
    return Object.values(state.value?.scores ?? {}).filter((score) => score.value !== null).length
})

const hasUnsyncedChanges = computed(() => {
    if (!state.value?.lastLocalUpdatedAt) {
        return false
    }

    if (!state.value.lastSyncedAt) {
        return true
    }

    return Date.parse(state.value.lastLocalUpdatedAt) > Date.parse(state.value.lastSyncedAt)
})

const missingScoreCount = computed(() => {
    return Math.max(0, expectedScoreCount.value - submittedScoreCount.value)
})

const completionPercent = computed(() => {
    if (expectedScoreCount.value === 0) {
        return 0
    }

    return Math.round((submittedScoreCount.value / expectedScoreCount.value) * 100)
})

const selectedTeams = computed(() => {
    if (!bootstrap.value) {
        return []
    }

    return selectedTeamIds.value
        .map((teamId) => bootstrap.value!.teams.find((team) => team.id === teamId))
        .filter((team) => team !== undefined)
})

const teamVisualsById = computed(() => {
    const result: Record<string, {
        hasColor: boolean
        style: Record<string, string>
    }> = {}

    if (!bootstrap.value) {
        return result
    }

    for (const team of bootstrap.value.teams) {
        const visual = detectTeamColorVisual(team.name)

        result[team.id] = {
            hasColor: visual.hasColor,
            style: visual.hasColor
                ? {
                    '--gss-team-color': visual.color,
                    '--gss-team-border': visual.border,
                    '--gss-team-soft': visual.soft,
                    '--gss-team-text': visual.text,
                }
                : {},
        }
    }

    return result
})

function getTeamVisualClass(teamId: string) {
    return {
        'evaluator-team-colorized': teamVisualsById.value[teamId]?.hasColor === true,
    }
}

function getTeamVisualStyle(teamId: string) {
    return teamVisualsById.value[teamId]?.style ?? {}
}


function setPassiveMessage(
    message: string,
    tone: 'info' | 'success' | 'warning' | 'danger' = 'info',
) {
    passiveTone.value = tone
    passiveMessage.value = message

    window.setTimeout(() => {
        if (passiveMessage.value === message) {
            passiveMessage.value = ''
        }
    }, 3000)
}

function setSuccessMessage(message: string) {
    successMessage.value = message

    window.setTimeout(() => {
        if (successMessage.value === message) {
            successMessage.value = ''
        }
    }, 3500)
}

function getScoreValue(teamId: string, eventId: string, indicatorId: string): number | '' {
    const key = makeScoreKey(teamId, eventId, indicatorId)
    return state.value?.scores[key]?.value ?? ''
}

function getCommentValue(teamId: string, eventId: string): string {
    const key = makeCommentKey(teamId, eventId)
    return state.value?.comments[key]?.commentText ?? ''
}

function selectEvent(eventId: string) {
    activeEventId.value = eventId
}

function ensureInitialTeamSelection() {
    if (!bootstrap.value || bootstrap.value.teams.length === 0) {
        selectedTeamIds.value = []
        return
    }

    const validTeamIds = new Set(bootstrap.value.teams.map((team) => team.id))
    const currentValidSelection = selectedTeamIds.value
        .filter((teamId) => validTeamIds.has(teamId))
        .slice(0, 2)

    if (currentValidSelection.length > 0) {
        selectedTeamIds.value = currentValidSelection
        return
    }

    selectedTeamIds.value = [bootstrap.value.teams[0].id]
}

function isTeamSelected(teamId: string) {
    return selectedTeamIds.value.includes(teamId)
}

function toggleTeamSelection(teamId: string) {
    const currentSelection = [...selectedTeamIds.value]
    const existingIndex = currentSelection.indexOf(teamId)

    if (existingIndex >= 0) {
        if (currentSelection.length === 1) {
            setPassiveMessage('حداقل یک تیم باید برای امتیازدهی انتخاب شده باشد.', 'warning')
            return
        }

        currentSelection.splice(existingIndex, 1)
        selectedTeamIds.value = currentSelection
        return
    }

    if (currentSelection.length >= 2) {
        selectedTeamIds.value = [currentSelection[1], teamId]
        return
    }

    selectedTeamIds.value = [...currentSelection, teamId]
}


async function loadEvaluation() {
    isLoading.value = true
    errorMessage.value = ''
    successMessage.value = ''
    validationMessage.value = ''

    try {
        if (navigator.onLine) {
            const remoteBootstrap = await getEvaluatorBootstrap(props.token)
            state.value = await saveBootstrapToLocal(props.token, remoteBootstrap)
            ensureInitialTeamSelection()

            if (!activeEventId.value && remoteBootstrap.events.length > 0) {
                activeEventId.value = remoteBootstrap.events[0].id
            }

            setSuccessMessage('فرم ارزیابی آماده است و روی همین دستگاه ذخیره شد.')
        } else {
            const localState = await getLocalEvaluation(props.token)

            if (!localState) {
                errorMessage.value =
                    'برای استفاده آفلاین، ابتدا باید این لینک را یک‌بار با اینترنت باز کنید.'
                return
            }

            state.value = localState
            ensureInitialTeamSelection()

            if (!activeEventId.value && localState.bootstrap.events.length > 0) {
                activeEventId.value = localState.bootstrap.events[0].id
            }

            setPassiveMessage('حالت آفلاین فعال است؛ اطلاعات از حافظه دستگاه نمایش داده شد.')
        }
    } catch (error) {
        const localState = await getLocalEvaluation(props.token)

        if (localState) {
            state.value = localState
            ensureInitialTeamSelection()

            if (!activeEventId.value && localState.bootstrap.events.length > 0) {
                activeEventId.value = localState.bootstrap.events[0].id
            }

            setPassiveMessage('اتصال برقرار نیست؛ نسخه ذخیره‌شده محلی نمایش داده شد.')
        } else {
            errorMessage.value = getApiErrorMessage(error)
        }
    } finally {
        isLoading.value = false
    }
}

async function handleScoreInput(
    teamId: string,
    eventId: string,
    indicatorId: string,
    rawValue: string,
) {
    if (isReadOnly.value) {
        return
    }

    validationMessage.value = ''

    const normalizedValue = rawValue.trim()

    if (normalizedValue === '') {
        state.value = await clearLocalScore(props.token, teamId, eventId, indicatorId)
        setPassiveMessage('امتیاز پاک شد و در ثبت موقت بعدی از سرور هم حذف می‌شود.', 'info')
        return
    }

    if (!/^\d+$/.test(normalizedValue)) {
        setPassiveMessage('امتیاز فقط باید عدد صحیح بین 0 تا 10 باشد.', 'danger')
        return
    }

    const value = Number(normalizedValue)

    if (!Number.isInteger(value) || value < 0 || value > 10) {
        setPassiveMessage('امتیاز باید بین 0 تا 10 باشد.', 'danger')
        return
    }

    state.value = await saveLocalScore(props.token, {
        teamId,
        eventId,
        indicatorId,
        value,
        clientUpdatedAt: new Date().toISOString(),
    })

    setPassiveMessage('ذخیره شد.', 'success')
}

async function handleCommentInput(teamId: string, eventId: string, commentText: string) {
    if (isReadOnly.value) {
        return
    }

    state.value = await saveLocalComment(props.token, {
        teamId,
        eventId,
        commentText,
        clientUpdatedAt: new Date().toISOString(),
    })

    setPassiveMessage('توضیح ذخیره شد.', 'success')
}

function buildSyncRequest(isFinalSync: boolean): EvaluatorSyncRequest {
    if (!state.value) {
        throw new Error('Evaluation state is not loaded.')
    }

    return {
        courseId: state.value.bootstrap.course.id,
        roundId: state.value.bootstrap.round.id,
        isFinalSync,
        clientSyncedAt: new Date().toISOString(),
        scores: Object.values(state.value.scores).map((score) => ({
            teamId: score.teamId,
            eventId: score.eventId,
            indicatorId: score.indicatorId,
            value: score.value,
            clientUpdatedAt: score.clientUpdatedAt,
        })),
        comments: Object.values(state.value.comments).map((comment) => ({
            teamId: comment.teamId,
            eventId: comment.eventId,
            commentText: comment.commentText,
            clientUpdatedAt: comment.clientUpdatedAt,
        })),
    }
}

async function handlePartialSync() {
    await handleSync(false)
}

function requestFinalSync() {
    if (missingScoreCount.value > 0) {
        setPassiveMessage(
            `برای ثبت نهایی، ${missingScoreCount.value} امتیاز هنوز ثبت نشده است.`,
            'danger',
        )
        return
    }

    if (!navigator.onLine) {
        setPassiveMessage(
            'آفلاین هستید؛ برای ثبت نهایی باید اتصال اینترنت برقرار باشد.',
            'warning',
        )
        return
    }

    isFinalConfirmModalOpen.value = true
}

function closeFinalConfirmModal() {
    if (isSyncing.value) {
        return
    }

    isFinalConfirmModalOpen.value = false
}

async function confirmFinalSync() {
    isFinalConfirmModalOpen.value = false
    await handleSync(true)
}

function closeSyncErrorModal() {
    syncErrorModal.value = null
}

async function handleSync(isFinalSync: boolean) {
    if (!state.value) {
        return
    }

    if (!navigator.onLine) {
        setPassiveMessage(
            'آفلاین هستید؛ امتیازها روی دستگاه ذخیره شده‌اند و بعداً ثبت موقت می‌شوند.',
            'warning',
        )
        return
    }

    isSyncing.value = true
    errorMessage.value = ''
    successMessage.value = ''
    validationMessage.value = ''

    try {
        const request = buildSyncRequest(isFinalSync)
        const response = await syncEvaluator(props.token, request)

        state.value = await markLocalSynced(
            props.token,
            response.isFinalized,
            response.serverSyncedAt,
        )

        setSuccessMessage(
            isFinalSync
                ? 'ثبت نهایی با موفقیت انجام شد. فرم قفل شد.'
                : 'همگام‌سازی موقت با موفقیت انجام شد.',
        )
    } catch (error) {
        const message = getApiErrorMessage(error)

        if (navigator.onLine) {
            syncErrorModal.value = {
                title: isFinalSync ? 'ثبت نهایی انجام نشد' : 'ثبت موقت انجام نشد',
                message,
            }
        } else {
            errorMessage.value = message
        }
    } finally {
        isSyncing.value = false
    }
}

function getEventSubmittedCount(eventId: string) {
    if (!state.value || !bootstrap.value) {
        return 0
    }

    let count = 0

    for (const team of bootstrap.value.teams) {
        const eventBlock = bootstrap.value.events.find((item) => item.id === eventId)

        if (!eventBlock) {
            continue
        }

        for (const indicator of eventBlock.indicators) {
            const key = makeScoreKey(team.id, eventId, indicator.id)

            if (state.value.scores[key]?.value !== null && state.value.scores[key]?.value !== undefined) {
                count++
            }
        }
    }

    return count
}

function getEventExpectedCount(eventId: string) {
    if (!bootstrap.value) {
        return 0
    }

    const eventBlock = bootstrap.value.events.find((item) => item.id === eventId)

    if (!eventBlock) {
        return 0
    }

    return bootstrap.value.teams.length * eventBlock.indicators.length
}

function getEventCompletionPercent(eventId: string) {
    const expected = getEventExpectedCount(eventId)

    if (expected === 0) {
        return 0
    }

    return Math.round((getEventSubmittedCount(eventId) / expected) * 100)
}

function getTeamEventSubmittedCount(teamId: string, eventId: string | null): number {
    if (!state.value || !bootstrap.value || !eventId) {
        return 0
    }

    const eventBlock = bootstrap.value.events.find((item) => item.id === eventId)

    if (!eventBlock) {
        return 0
    }

    let count = 0

    for (const indicator of eventBlock.indicators) {
        const key = makeScoreKey(teamId, eventId, indicator.id)

        if (state.value.scores[key]?.value !== null && state.value.scores[key]?.value !== undefined) {
            count++
        }
    }

    return count
}

function getTeamEventExpectedCount(eventId: string | null): number {
    if (!bootstrap.value || !eventId) {
        return 0
    }

    const eventBlock = bootstrap.value.events.find((item) => item.id === eventId)

    return eventBlock?.indicators.length ?? 0
}

function getTeamEventCompletionPercent(teamId: string, eventId: string | null): number {
    const expected = getTeamEventExpectedCount(eventId)

    if (expected === 0) {
        return 0
    }

    return Math.round((getTeamEventSubmittedCount(teamId, eventId) / expected) * 100)
}


function handleOnline() {
    isOnline.value = true

    if (hasUnsyncedChanges.value) {
        setPassiveMessage('اتصال برقرار شد؛ برای ارسال تغییرات ذخیره‌شده، ثبت موقت را بزنید.', 'info')
        return
    }

    setPassiveMessage('اتصال برقرار شد. می‌توانید ثبت موقت کنید.')
}

function handleOffline() {
    isOnline.value = false
    setPassiveMessage('آفلاین شدید؛ ادامه دهید، تغییرات روی دستگاه ذخیره می‌شود.', 'warning')
}

onMounted(() => {
    window.addEventListener('online', handleOnline)
    window.addEventListener('offline', handleOffline)

    loadEvaluation()
})

onUnmounted(() => {
    window.removeEventListener('online', handleOnline)
    window.removeEventListener('offline', handleOffline)
})
</script>

<template>
    <main class="gss-page evaluator-mobile-page">
        <div class="gss-container-wide">
            <header class="evaluator-topbar">
                <div class="evaluator-heading-block">
                    <h1 class="evaluator-title">
                        فرم ارزیابی
                    </h1>

                    <p v-if="bootstrap" class="evaluator-subtitle">
                        {{ bootstrap.course.organizerCompanyName }}
                    </p>

                    <p v-if="bootstrap" class="evaluator-subtitle">
                        {{ bootstrap.evaluator.evaluatorName }}
                        |
                        Round {{ bootstrap.round.roundNumber }}
                    </p>
                </div>

                <div class="evaluator-header-logo" aria-label="پردیس نوآوری گرا">
                    <img src="/evaluator-logo.png" alt="پردیس نوآوری گرا" />
                </div>

                <div class="evaluator-status-stack">
                    <span class="gss-badge" :class="isOnline ? 'gss-badge-success' : 'gss-badge-warning'">
                        <i :class="isOnline ? 'bi bi-wifi' : 'bi bi-wifi-off'"></i>
                        {{ isOnline ? 'Online' : 'Offline' }}
                    </span>

                    <span class="gss-badge" :class="isReadOnly ? 'gss-badge-muted' : 'gss-badge-success'">
                        {{ isReadOnly ? 'Read-only' : 'Active' }}
                    </span>
                </div>
            </header>

            <div v-if="passiveMessage" class="evaluator-toast" :class="`evaluator-toast-${passiveTone}`">
                <i :class="passiveTone === 'danger'
                    ? 'bi bi-exclamation-triangle'
                    : passiveTone === 'success'
                        ? 'bi bi-check-circle'
                        : passiveTone === 'warning'
                            ? 'bi bi-wifi-off'
                            : 'bi bi-info-circle'"></i>
                <span>{{ passiveMessage }}</span>
            </div>

            <div v-if="!isOnline" class="gss-offline-pill">
                <i class="bi bi-wifi-off"></i>
                آفلاین هستید؛ امتیازها روی دستگاه ذخیره می‌شوند.
            </div>

            <section v-if="errorMessage" class="gss-alert gss-alert-danger">
                <i class="bi bi-exclamation-triangle"></i>
                <span>{{ errorMessage }}</span>
            </section>

            <section v-if="successMessage" class="gss-alert gss-alert-success">
                <i class="bi bi-check-circle"></i>
                <span>{{ successMessage }}</span>
            </section>

            <section v-if="isLoading" class="gss-card">
                <div class="gss-card-body evaluator-loading">
                    <div class="spinner-border text-primary mb-3" role="status"></div>
                    <div>در حال آماده‌سازی فرم ارزیابی...</div>
                </div>
            </section>

            <section v-else-if="!state || !bootstrap" class="gss-card">
                <div class="gss-card-body evaluator-loading">
                    اطلاعات ارزیابی قابل نمایش نیست.
                </div>
            </section>

            <template v-else>
                <section class="evaluator-progress-card">
                    <div class="d-flex align-items-center justify-content-between gap-3 mb-2">
                        <div>
                            <div class="evaluator-progress-title">
                                پیشرفت ثبت امتیازها
                            </div>
                            <div class="evaluator-progress-subtitle">
                                {{ submittedScoreCount }} از {{ expectedScoreCount }} امتیاز ثبت شده
                            </div>
                        </div>

                        <div class="evaluator-progress-percent">
                            {{ completionPercent }}٪
                        </div>
                    </div>

                    <div class="progress evaluator-progress" role="progressbar" :aria-valuenow="completionPercent"
                        aria-valuemin="0" aria-valuemax="100">
                        <div class="progress-bar" :style="{ width: `${completionPercent}%` }"></div>
                    </div>

                    <div class="evaluator-progress-meta">
                        <!--<span>
                            باقی‌مانده:
                            <strong>{{ missingScoreCount }}</strong>
                        </span>-->
                        <span>
                            آخرین ثبت موقت انجام شده:
                            <strong>{{ state.lastSyncedAt ? new Date(state.lastSyncedAt).toLocaleString('fa-IR') : '-'
                            }}</strong>
                        </span>

                        <span>
                            وضعیت:
                            <strong>{{ hasUnsyncedChanges ? 'تغییرات روی دستگاه ذخیره شده و هنوز ارسال نشده است' : 'هماهنگ با سرور' }}</strong>
                        </span>
                    </div>
                </section>

                <nav class="evaluator-event-tabs" aria-label="رویدادها">
                    <button v-for="eventBlock in bootstrap.events" :key="eventBlock.id" class="evaluator-event-tab"
                        type="button" :class="{ active: activeEventId === eventBlock.id }"
                        :aria-pressed="activeEventId === eventBlock.id"
                        @click="selectEvent(eventBlock.id)">
                        <span>{{ eventBlock.name }}</span>
                        <small>
                            {{ getEventCompletionPercent(eventBlock.id) }}٪
                        </small>
                    </button>
                </nav>

                <section class="evaluator-team-picker-card" aria-label="انتخاب تیم‌ها">
                    <div class="evaluator-team-picker-header">
                        <div>
                            <h2>انتخاب تیم برای امتیازدهی</h2>
                            <p>حداکثر دو تیم را می‌توانید هم‌زمان انتخاب کنید.</p>
                        </div>

                        <span class="gss-badge gss-badge-muted">
                            {{ selectedTeams.length }}/2 انتخاب
                        </span>
                    </div>

                    <div class="evaluator-team-pick-strip">
                        <button
                            v-for="team in bootstrap.teams"
                            :key="team.id"
                            class="evaluator-team-pick-chip"
                            type="button"
                            :class="[
                                { active: isTeamSelected(team.id) },
                                getTeamVisualClass(team.id),
                            ]"
                            :style="getTeamVisualStyle(team.id)"
                            :aria-pressed="isTeamSelected(team.id)"
                            @click="toggleTeamSelection(team.id)"
                        >
                            <span class="evaluator-team-pick-check">
                                <i v-if="isTeamSelected(team.id)" class="bi bi-check2"></i>
                            </span>

                            <span class="evaluator-team-pick-content">
                                <strong>{{ team.name }}</strong>
                                <small v-if="activeEventId">
                                    {{ getTeamEventCompletionPercent(team.id, activeEventId) }}٪ این رویداد
                                </small>
                            </span>
                        </button>
                    </div>
                </section>

                <section v-for="eventBlock in bootstrap.events" v-show="activeEventId === eventBlock.id"
                    :key="eventBlock.id" class="evaluator-event-section">
                    <div class="evaluator-event-header">
                        <div>
                            <h2>{{ eventBlock.name }}</h2>
                            <p>
                                {{ eventBlock.indicators.length }} شاخص
                                —
                                {{ selectedTeams.length }} تیم فعال از {{ bootstrap.teams.length }} تیم
                            </p>
                        </div>

                        <span class="gss-badge">
                            {{ getEventSubmittedCount(eventBlock.id) }}/{{ getEventExpectedCount(eventBlock.id) }}
                        </span>
                    </div>

                    <div v-if="selectedTeams.length === 0" class="evaluator-no-team-selected">
                        حداقل یک تیم را برای امتیازدهی انتخاب کنید.
                    </div>

                    <div v-else class="evaluator-selected-team-grid">
                        <article v-for="team in selectedTeams" :key="`${eventBlock.id}-${team.id}`" :class="[
                            'evaluator-team-card',
                            'evaluator-team-card-active',
                            getTeamVisualClass(team.id),
                        ]" :style="getTeamVisualStyle(team.id)">
                            <div class="evaluator-team-header">
                                <div>
                                    <h3>{{ team.name }}</h3>
                                    <small>
                                        {{ getTeamEventSubmittedCount(team.id, eventBlock.id) }}/{{ getTeamEventExpectedCount(eventBlock.id) }}
                                        امتیاز این رویداد
                                    </small>
                                </div>

                                <span>تیم {{ team.displayOrder }}</span>
                            </div>

                            <div class="evaluator-score-grid">
                                <label v-for="indicator in eventBlock.indicators" :key="indicator.id"
                                    class="evaluator-score-field">
                                    <span>{{ indicator.name }}</span>

                                    <input class="evaluator-score-input" type="tel" inputmode="numeric" pattern="[0-9]*"
                                        autocomplete="off" maxlength="2" placeholder="0-10" :disabled="isReadOnly"
                                        :value="getScoreValue(team.id, eventBlock.id, indicator.id)" @input="handleScoreInput(
                                            team.id,
                                            eventBlock.id,
                                            indicator.id,
                                            ($event.target as HTMLInputElement).value
                                        )" />
                                </label>
                            </div>

                            <div class="mt-3">
                                <label class="evaluator-comment-label">
                                    توضیح اختیاری
                                </label>

                                <textarea class="evaluator-comment-input" rows="2" :disabled="isReadOnly"
                                    :value="getCommentValue(team.id, eventBlock.id)"
                                    placeholder="در صورت نیاز توضیح کوتاه وارد کنید..." @input="handleCommentInput(
                                        team.id,
                                        eventBlock.id,
                                        ($event.target as HTMLTextAreaElement).value
                                    )" />
                            </div>
                        </article>
                    </div>
                </section>

                <section class="evaluator-bottom-spacer"></section>

                <section class="evaluator-actionbar">
                    <div class="evaluator-actionbar-inner">
                        <div class="evaluator-actionbar-status">
                            <strong>{{ submittedScoreCount }}/{{ expectedScoreCount }}</strong>
                            <span>ثبت‌شده</span>
                        </div>

                        <div class="evaluator-actionbar-buttons">
                            <button class="btn btn-light border" type="button" :disabled="isSyncing || isReadOnly"
                                @click="handlePartialSync">
                                <span v-if="isSyncing" class="spinner-border spinner-border-sm ms-1"
                                    aria-hidden="true"></span>
                                ثبت موقت
                            </button>

                            <button class="btn btn-primary" type="button" :disabled="isSyncing || isReadOnly"
                                @click="requestFinalSync">
                                ثبت نهایی
                            </button>
                        </div>
                    </div>
                </section>
            </template>
        </div>

        <Teleport to="body">
            <div v-if="isFinalConfirmModalOpen" class="gss-modal-backdrop" role="dialog" aria-modal="true" @click.self="closeFinalConfirmModal">
                <div class="gss-modal">
                    <div class="gss-modal-icon warning">
                        <i class="bi bi-exclamation-triangle"></i>
                    </div>

                    <h3 class="gss-modal-title">ثبت نهایی ارزیابی</h3>

                    <p class="gss-modal-text">
                        آیا از ثبت نهایی امتیازها مطمئن هستید؟
                    </p>

                    <p class="gss-modal-warning-text">
                        بعد از ثبت نهایی، امکان ویرایش امتیازها و توضیحات وجود نخواهد داشت.
                    </p>

                    <div class="gss-modal-actions">
                        <button class="btn btn-primary" type="button" :disabled="isSyncing" @click="confirmFinalSync">
                            <span v-if="isSyncing" class="spinner-border spinner-border-sm ms-1" aria-hidden="true"></span>
                            بله، ثبت نهایی شود
                        </button>

                        <button class="btn btn-light border" type="button" :disabled="isSyncing" @click="closeFinalConfirmModal">
                            انصراف
                        </button>
                    </div>
                </div>
            </div>
        </Teleport>

        <Teleport to="body">
            <div v-if="syncErrorModal" class="gss-modal-backdrop" role="dialog" aria-modal="true" @click.self="closeSyncErrorModal">
                <div class="gss-modal">
                    <div class="gss-modal-icon danger">
                        <i class="bi bi-x-octagon"></i>
                    </div>

                    <h3 class="gss-modal-title">{{ syncErrorModal.title }}</h3>

                    <p class="gss-modal-text gss-modal-error-text">
                        {{ syncErrorModal.message }}
                    </p>

                    <div class="gss-modal-actions gss-modal-actions-single">
                        <button class="btn btn-danger" type="button" @click="closeSyncErrorModal">
                            متوجه شدم
                        </button>
                    </div>
                </div>
            </div>
        </Teleport>
    </main>
</template>
