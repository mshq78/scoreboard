<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import QRCode from 'qrcode'
import {
    createEvaluator,
    getCourseDetails,
    getEvaluators,
    getExportUrl,
    getScoreSheet,
    resetCourse,
    unfinalizeEvaluator,
} from '@/api/adminApi'
import { getAdminToken, getApiErrorMessage } from '@/api/http'
import type {
    AdminScoreSheetDto,
    CourseDetailsDto,
    EvaluatorProfileDto,
} from '@/types/admin'

const props = defineProps<{
    courseId: string
}>()

const course = ref<CourseDetailsDto | null>(null)
const evaluators = ref<EvaluatorProfileDto[]>([])
const scoreSheet = ref<AdminScoreSheetDto | null>(null)

const isLoading = ref(false)
const isCreatingEvaluator = ref(false)
const isResetting = ref(false)
const isDownloading = ref(false)
const isGeneratingQr = ref(false)
const isUnfinalizingEvaluatorId = ref<string | null>(null)

const errorMessage = ref('')
const successMessage = ref('')
const passiveMessage = ref('')

const newEvaluatorName = ref('')

const qrModal = ref<{
    evaluator: EvaluatorProfileDto
    dataUrl: string
} | null>(null)

const unfinalizeConfirmEvaluator = ref<EvaluatorProfileDto | null>(null)
const isResetConfirmOpen = ref(false)

const exportUrl = computed(() => getExportUrl(props.courseId))

const finalizedCount = computed(() => {
    return evaluators.value.filter((item) => item.status === 'Finalized').length
})

const activeCount = computed(() => {
    return evaluators.value.filter((item) => item.status === 'Active').length
})

async function loadAll() {
    isLoading.value = true
    errorMessage.value = ''
    successMessage.value = ''

    try {
        const [courseData, evaluatorData, scoreSheetData] = await Promise.all([
            getCourseDetails(props.courseId),
            getEvaluators(props.courseId),
            getScoreSheet(props.courseId),
        ])

        course.value = courseData
        evaluators.value = evaluatorData
        scoreSheet.value = scoreSheetData
    } catch (error) {
        errorMessage.value = getApiErrorMessage(error)
    } finally {
        isLoading.value = false
    }
}

async function handleCreateEvaluator() {
    errorMessage.value = ''
    successMessage.value = ''
    passiveMessage.value = ''

    isCreatingEvaluator.value = true

    try {
        await createEvaluator(props.courseId, {
            evaluatorName: newEvaluatorName.value,
        })

        newEvaluatorName.value = ''
        successMessage.value = 'لینک ارزیاب با موفقیت ساخته شد.'
        await loadAll()
    } catch (error) {
        errorMessage.value = getApiErrorMessage(error)
    } finally {
        isCreatingEvaluator.value = false
    }
}

function openResetConfirm() {
    errorMessage.value = ''
    successMessage.value = ''
    passiveMessage.value = ''
    isResetConfirmOpen.value = true
}

function closeResetConfirm() {
    if (isResetting.value) {
        return
    }

    isResetConfirmOpen.value = false
}

async function confirmReset() {
    errorMessage.value = ''
    successMessage.value = ''
    passiveMessage.value = ''

    isResetting.value = true

    try {
        await resetCourse(props.courseId, {
            resetReason: 'Reset from admin UI',
        })

        isResetConfirmOpen.value = false
        successMessage.value = 'Reset با موفقیت انجام شد.'
        await loadAll()
    } catch (error) {
        errorMessage.value = getApiErrorMessage(error)
    } finally {
        isResetting.value = false
    }
}

async function copyToClipboard(text: string) {
    try {
        await navigator.clipboard.writeText(text)
        passiveMessage.value = 'لینک کپی شد.'
        setTimeout(() => {
            passiveMessage.value = ''
        }, 2500)
    } catch {
        passiveMessage.value = 'کپی خودکار انجام نشد. لینک را دستی کپی کنید.'
    }
}


async function openQrModal(evaluator: EvaluatorProfileDto) {
    errorMessage.value = ''
    passiveMessage.value = ''
    isGeneratingQr.value = true

    try {
        const dataUrl = await QRCode.toDataURL(evaluator.evaluatorUrl, {
            errorCorrectionLevel: 'M',
            margin: 1,
            width: 280,
        })

        qrModal.value = {
            evaluator,
            dataUrl,
        }
    } catch (error) {
        errorMessage.value = error instanceof Error
            ? error.message
            : 'خطا در ساخت QRCode لینک ارزیاب.'
    } finally {
        isGeneratingQr.value = false
    }
}

function closeQrModal() {
    if (isGeneratingQr.value) {
        return
    }

    qrModal.value = null
}

function openUnfinalizeConfirm(evaluator: EvaluatorProfileDto) {
    if (evaluator.status !== 'Finalized') {
        passiveMessage.value = 'فقط داور نهایی‌شده را می‌توان غیر نهایی کرد.'
        setTimeout(() => {
            if (passiveMessage.value === 'فقط داور نهایی‌شده را می‌توان غیر نهایی کرد.') {
                passiveMessage.value = ''
            }
        }, 2500)
        return
    }

    unfinalizeConfirmEvaluator.value = evaluator
}

function closeUnfinalizeConfirm() {
    if (isUnfinalizingEvaluatorId.value) {
        return
    }

    unfinalizeConfirmEvaluator.value = null
}

async function confirmUnfinalizeEvaluator() {
    const evaluator = unfinalizeConfirmEvaluator.value

    if (!evaluator) {
        return
    }

    errorMessage.value = ''
    successMessage.value = ''
    passiveMessage.value = ''
    isUnfinalizingEvaluatorId.value = evaluator.id

    try {
        await unfinalizeEvaluator(props.courseId, evaluator.id)
        successMessage.value = 'فرم داور از حالت نهایی خارج شد و دوباره قابل ویرایش است.'
        unfinalizeConfirmEvaluator.value = null
        await loadAll()
    } catch (error) {
        errorMessage.value = getApiErrorMessage(error)
    } finally {
        isUnfinalizingEvaluatorId.value = null
    }
}

async function downloadExport() {
    const token = getAdminToken()

    if (!token) {
        errorMessage.value = 'Admin Token ثبت نشده است.'
        return
    }

    isDownloading.value = true
    errorMessage.value = ''
    successMessage.value = ''

    try {
        const response = await fetch(exportUrl.value, {
            headers: {
                'X-Admin-Token': token,
            },
        })

        if (!response.ok) {
            throw new Error(await response.text())
        }

        const blob = await response.blob()
        const url = window.URL.createObjectURL(blob)
        const link = document.createElement('a')

        link.href = url
        link.download = 'groups-score-sheet-output.xlsx'
        link.click()

        window.URL.revokeObjectURL(url)
    } catch (error) {
        errorMessage.value = error instanceof Error ? error.message : 'خطا در دانلود خروجی'
    } finally {
        isDownloading.value = false
    }
}

function formatDate(value: string | null | undefined) {
    if (!value) {
        return '-'
    }

    return new Date(value).toLocaleDateString('fa-IR')
}

function formatDateTime(value: string | null | undefined) {
    if (!value) {
        return '-'
    }

    return new Date(value).toLocaleString('fa-IR')
}

function getEvaluatorBadgeClass(status: string) {
    if (status === 'Finalized') {
        return 'gss-badge-success'
    }

    if (status === 'Invalidated') {
        return 'gss-badge-danger'
    }

    return 'gss-badge-warning'
}

type RankableScoreRow = {
    teamId: string
    total: number | null | undefined
}

type AdminFinalScoreRow = {
    teamId: string
    teamName: string
    total: number | null
    average: number | null
    scoredEventCount: number
    rank: number | null
    order: number
}

const eventRankMaps = computed(() => {
    const rankMaps: Record<string, Map<string, number>> = {}

    if (!scoreSheet.value) {
        return rankMaps
    }

    for (const eventBlock of scoreSheet.value.events) {
        rankMaps[eventBlock.eventId] = buildRankMap(eventBlock.rows)
    }

    return rankMaps
})

const finalScoreRows = computed<AdminFinalScoreRow[]>(() => {
    if (!scoreSheet.value) {
        return []
    }

    const teamScoreMap = new Map<string, {
        teamId: string
        teamName: string
        totalSum: number
        averageSum: number
        totalEventCount: number
        averageEventCount: number
        order: number
    }>()

    let order = 0

    for (const eventBlock of scoreSheet.value.events) {
        for (const row of eventBlock.rows) {
            let teamScore = teamScoreMap.get(row.teamId)

            if (!teamScore) {
                teamScore = {
                    teamId: row.teamId,
                    teamName: row.teamName,
                    totalSum: 0,
                    averageSum: 0,
                    totalEventCount: 0,
                    averageEventCount: 0,
                    order,
                }

                teamScoreMap.set(row.teamId, teamScore)
                order++
            }

            if (isFiniteScore(row.total)) {
                teamScore.totalSum += row.total
                teamScore.totalEventCount++
            }

            if (isFiniteScore(row.average)) {
                teamScore.averageSum += row.average
                teamScore.averageEventCount++
            }
        }
    }

    const rows: AdminFinalScoreRow[] = Array.from(teamScoreMap.values()).map((item) => {
        const total = item.totalEventCount > 0
            ? roundScore(item.totalSum)
            : null

        const average = item.averageEventCount > 0
            ? roundScore(item.averageSum / item.averageEventCount)
            : null

        return {
            teamId: item.teamId,
            teamName: item.teamName,
            total,
            average,
            scoredEventCount: item.averageEventCount,
            rank: null,
            order: item.order,
        }
    })

    const rankMap = buildRankMap(rows)

    return rows
        .map((row) => ({
            ...row,
            rank: rankMap.get(row.teamId) ?? null,
        }))
        .sort((first, second) => {
            const firstTotal = isFiniteScore(first.total) ? first.total : null
            const secondTotal = isFiniteScore(second.total) ? second.total : null

            if (firstTotal !== null && secondTotal !== null) {
                if (secondTotal !== firstTotal) {
                    return secondTotal - firstTotal
                }

                return first.order - second.order
            }

            if (firstTotal !== null) {
                return -1
            }

            if (secondTotal !== null) {
                return 1
            }

            return first.order - second.order
        })
})

function getEventTeamRank(eventId: string, teamId: string) {
    return eventRankMaps.value[eventId]?.get(teamId) ?? null
}

function getRankBadgeClass(rank: number | null | undefined) {
    if (rank === 1) {
        return 'admin-rank-badge-gold'
    }

    if (rank === 2) {
        return 'admin-rank-badge-silver'
    }

    if (rank === 3) {
        return 'admin-rank-badge-bronze'
    }

    return ''
}

function buildRankMap<T extends RankableScoreRow>(rows: T[]) {
    const rankMap = new Map<string, number>()

    const sortedRows = rows
        .filter((row) => isFiniteScore(row.total))
        .map((row) => ({
            teamId: row.teamId,
            total: roundScore(row.total as number),
        }))
        .sort((first, second) => second.total - first.total)

    let currentRank = 0
    let previousTotal: number | null = null

    for (const row of sortedRows) {
        if (previousTotal === null || row.total !== previousTotal) {
            currentRank++
            previousTotal = row.total
        }

        if (currentRank > 3) {
            break
        }

        rankMap.set(row.teamId, currentRank)
    }

    return rankMap
}

function isFiniteScore(value: number | null | undefined): value is number {
    return typeof value === 'number' && Number.isFinite(value)
}

function roundScore(value: number) {
    return Math.round((value + Number.EPSILON) * 100) / 100
}

function formatScore(value: number | null | undefined) {
    if (!isFiniteScore(value)) {
        return '-'
    }

    const roundedValue = roundScore(value)

    if (Number.isInteger(roundedValue)) {
        return String(roundedValue)
    }

    return roundedValue
        .toFixed(2)
        .replace(/0+$/, '')
        .replace(/\.$/, '')
}

onMounted(loadAll)
</script>

<template>
    <main class="gss-page admin-details-page">
        <div class="gss-container-wide">
            <header class="gss-header">
                <div>
                    <h1 class="gss-title">
                        جزئیات دوره
                    </h1>

                    <p v-if="course" class="gss-subtitle">
                        {{ course.organizerCompanyName }}
                        —
                        {{ formatDate(course.holdingDate) }}
                    </p>
                </div>

                <div class="d-grid d-sm-flex gap-2">
                    <button class="btn btn-outline-danger" type="button" :disabled="isResetting" @click="openResetConfirm">
                        <span v-if="isResetting" class="spinner-border spinner-border-sm ms-1"
                            aria-hidden="true"></span>
                        <i v-else class="bi bi-arrow-counterclockwise ms-1"></i>
                        Reset
                    </button>

                    <button class="btn btn-primary" type="button" :disabled="isDownloading" @click="downloadExport">
                        <span v-if="isDownloading" class="spinner-border spinner-border-sm ms-1"
                            aria-hidden="true"></span>
                        <i v-else class="bi bi-file-earmark-excel ms-1"></i>
                        خروجی Excel
                    </button>

                    <button class="btn btn-light border" type="button" :disabled="isLoading" @click="loadAll">
                        <i class="bi bi-arrow-clockwise ms-1"></i>
                        بروزرسانی
                    </button>

                    <RouterLink class="btn btn-light border" to="/admin">
                        <i class="bi bi-arrow-right ms-1"></i>
                        بازگشت
                    </RouterLink>
                </div>
            </header>

            <div v-if="errorMessage" class="gss-alert gss-alert-danger">
                <i class="bi bi-exclamation-triangle"></i>
                <span>{{ errorMessage }}</span>
            </div>

            <div v-if="successMessage" class="gss-alert gss-alert-success">
                <i class="bi bi-check-circle"></i>
                <span>{{ successMessage }}</span>
            </div>

            <div v-if="passiveMessage" class="gss-alert gss-alert-info admin-passive-alert">
                <i class="bi bi-info-circle"></i>
                <span>{{ passiveMessage }}</span>
            </div>

            <section v-if="isLoading" class="gss-card">
                <div class="gss-card-body admin-empty-state">
                    <div class="spinner-border text-primary mb-3" role="status"></div>
                    <div>در حال دریافت اطلاعات...</div>
                </div>
            </section>

            <template v-else>
                <section v-if="course" class="admin-stats-grid mb-3">
                    <div class="admin-stat-card">
                        <div class="admin-stat-icon">
                            <i class="bi bi-people"></i>
                        </div>
                        <div>
                            <div class="admin-stat-value">{{ course.teams.length }}</div>
                            <div class="admin-stat-label">تیم</div>
                        </div>
                    </div>

                    <div class="admin-stat-card">
                        <div class="admin-stat-icon">
                            <i class="bi bi-diagram-3"></i>
                        </div>
                        <div>
                            <div class="admin-stat-value">{{ course.events.length }}</div>
                            <div class="admin-stat-label">رویداد</div>
                        </div>
                    </div>

                    <div class="admin-stat-card">
                        <div class="admin-stat-icon">
                            <i class="bi bi-person-check"></i>
                        </div>
                        <div>
                            <div class="admin-stat-value">{{ evaluators.length }}</div>
                            <div class="admin-stat-label">ارزیاب</div>
                        </div>
                    </div>

                    <div class="admin-stat-card">
                        <div class="admin-stat-icon">
                            <i class="bi bi-check2-circle"></i>
                        </div>
                        <div>
                            <div class="admin-stat-value">{{ finalizedCount }}</div>
                            <div class="admin-stat-label">نهایی‌شده</div>
                        </div>
                    </div>
                </section>

                <section v-if="course" class="gss-card mb-3">
                    <div class="gss-card-header">
                        <h2 class="gss-card-title">
                            <i class="bi bi-info-circle ms-1"></i>
                            خلاصه دوره
                        </h2>
                    </div>

                    <div class="gss-card-body">
                        <div class="row g-3">
                            <div class="col-12 col-md-6 col-xl-3">
                                <div class="admin-info-item">
                                    <span>شرکت</span>
                                    <strong>{{ course.organizerCompanyName }}</strong>
                                </div>
                            </div>

                            <div class="col-12 col-md-6 col-xl-3">
                                <div class="admin-info-item">
                                    <span>تاریخ</span>
                                    <strong>{{ formatDate(course.holdingDate) }}</strong>
                                </div>
                            </div>

                            <div class="col-12 col-md-6 col-xl-3">
                                <div class="admin-info-item">
                                    <span>Round فعال</span>
                                    <strong>Round {{ course.activeRound?.roundNumber }}</strong>
                                </div>
                            </div>

                            <div class="col-12 col-md-6 col-xl-3">
                                <div class="admin-info-item">
                                    <span>وضعیت Round</span>
                                    <strong>{{ course.activeRound?.status }}</strong>
                                </div>
                            </div>
                        </div>
                    </div>
                </section>

                <section class="gss-card mb-3">
                    <div class="gss-card-header">
                        <div class="d-flex align-items-start justify-content-between gap-3">
                            <div>
                                <h2 class="gss-card-title">
                                    <i class="bi bi-link-45deg ms-1"></i>
                                    لینک‌های ارزیاب‌ها
                                </h2>
                                <p class="gss-card-subtitle">
                                    هر لینک یک EvaluatorProfile مستقل در DB دارد.
                                </p>
                            </div>

                            <span class="gss-badge gss-badge-muted">
                                Active: {{ activeCount }}
                            </span>
                        </div>
                    </div>

                    <div class="gss-card-body">
                        <div class="row g-2 align-items-end mb-3">
                            <div class="col-12 col-md">
                                <label class="form-label">نام ارزیاب</label>
                                <input v-model="newEvaluatorName" class="form-control" placeholder="مثلاً ارزیاب اول"
                                    autocomplete="off" @keyup.enter="handleCreateEvaluator" />
                            </div>

                            <div class="col-12 col-md-auto">
                                <button class="btn btn-primary w-100" type="button" :disabled="isCreatingEvaluator"
                                    @click="handleCreateEvaluator">
                                    <span v-if="isCreatingEvaluator" class="spinner-border spinner-border-sm ms-1"
                                        aria-hidden="true"></span>
                                    ساخت لینک ارزیاب
                                </button>
                            </div>
                        </div>

                        <div v-if="evaluators.length === 0" class="admin-empty-state">
                            <i class="bi bi-person-plus admin-empty-icon"></i>
                            <div class="fw-bold">هنوز برای Round فعال ارزیابی ساخته نشده است.</div>
                        </div>

                        <template v-else>
                            <!-- Mobile cards -->
                            <div class="d-lg-none admin-evaluator-list-mobile">
                                <article v-for="evaluator in evaluators" :key="evaluator.id"
                                    class="admin-evaluator-card">
                                    <div class="d-flex align-items-start justify-content-between gap-2">
                                        <div>
                                            <h3 class="admin-course-title">
                                                {{ evaluator.evaluatorName }}
                                            </h3>
                                            <div class="admin-course-meta">
                                                Round {{ evaluator.roundNumber }}
                                            </div>
                                        </div>

                                        <span class="gss-badge" :class="getEvaluatorBadgeClass(evaluator.status)">
                                            {{ evaluator.status }}
                                        </span>
                                    </div>

                                    <div class="admin-evaluator-meta-grid mt-3">
                                        <div>
                                            <span>آخرین Sync</span>
                                            <strong>{{ formatDateTime(evaluator.lastSyncedAt) }}</strong>
                                        </div>
                                        <div>
                                            <span>ثبت نهایی</span>
                                            <strong>{{ formatDateTime(evaluator.finalSyncedAt) }}</strong>
                                        </div>
                                    </div>

                                    <div class="admin-evaluator-actions mt-3">
                                        <button class="btn btn-light border" type="button"
                                            @click="copyToClipboard(evaluator.evaluatorUrl)">
                                            کپی آدرس
                                        </button>

                                        <a class="btn btn-outline-primary" :href="evaluator.evaluatorUrl"
                                            target="_blank">
                                            باز کردن صفحه
                                        </a>

                                        <button class="btn btn-light border" type="button"
                                            :disabled="isGeneratingQr"
                                            @click="openQrModal(evaluator)">
                                            نمایش QR لینک
                                        </button>

                                        <button class="btn btn-outline-danger" type="button"
                                            :disabled="evaluator.status !== 'Finalized' || isUnfinalizingEvaluatorId === evaluator.id"
                                            @click="openUnfinalizeConfirm(evaluator)">
                                            <span v-if="isUnfinalizingEvaluatorId === evaluator.id"
                                                class="spinner-border spinner-border-sm ms-1"
                                                aria-hidden="true"></span>
                                            غیر نهایی کردن
                                        </button>
                                    </div>
                                </article>
                            </div>

                            <!-- Desktop table -->
                            <div class="d-none d-lg-block gss-table-scroll">
                                <table class="table table-hover align-middle">
                                    <thead>
                                        <tr>
                                            <th>نام ارزیاب</th>
                                            <th>Round</th>
                                            <th>وضعیت</th>
                                            <th>اولین باز شدن</th>
                                            <th>آخرین Sync</th>
                                            <th>ثبت نهایی</th>
                                            <th class="text-center">لینک</th>
                                        </tr>
                                    </thead>

                                    <tbody>
                                        <tr v-for="evaluator in evaluators" :key="evaluator.id">
                                            <td class="fw-bold">{{ evaluator.evaluatorName }}</td>
                                            <td>Round {{ evaluator.roundNumber }}</td>
                                            <td>
                                                <span class="gss-badge"
                                                    :class="getEvaluatorBadgeClass(evaluator.status)">
                                                    {{ evaluator.status }}
                                                </span>
                                            </td>
                                            <td>{{ formatDateTime(evaluator.firstOpenedAt) }}</td>
                                            <td>{{ formatDateTime(evaluator.lastSyncedAt) }}</td>
                                            <td>{{ formatDateTime(evaluator.finalSyncedAt) }}</td>
                                            <td>
                                                <div class="admin-evaluator-actions admin-evaluator-actions-table">
                                                    <button class="btn btn-sm btn-light border" type="button"
                                                        @click="copyToClipboard(evaluator.evaluatorUrl)">
                                                        کپی آدرس
                                                    </button>

                                                    <a class="btn btn-sm btn-outline-primary"
                                                        :href="evaluator.evaluatorUrl" target="_blank">
                                                        باز کردن صفحه
                                                    </a>

                                                    <button class="btn btn-sm btn-light border" type="button"
                                                        :disabled="isGeneratingQr"
                                                        @click="openQrModal(evaluator)">
                                                        نمایش QR لینک
                                                    </button>

                                                    <button class="btn btn-sm btn-outline-danger" type="button"
                                                        :disabled="evaluator.status !== 'Finalized' || isUnfinalizingEvaluatorId === evaluator.id"
                                                        @click="openUnfinalizeConfirm(evaluator)">
                                                        <span v-if="isUnfinalizingEvaluatorId === evaluator.id"
                                                            class="spinner-border spinner-border-sm ms-1"
                                                            aria-hidden="true"></span>
                                                        غیر نهایی کردن
                                                    </button>
                                                </div>
                                            </td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                        </template>
                    </div>
                </section>

                <section class="gss-card">
                    <div class="gss-card-header">
                        <div class="d-flex align-items-start justify-content-between gap-3">
                            <div>
                                <h2 class="gss-card-title">
                                    <i class="bi bi-table ms-1"></i>
                                    نمایش امتیازها
                                </h2>
                                <p class="gss-card-subtitle">
                                    هر امتیازی که با ثبت موقت یا ثبت نهایی به سرور رسیده باشد، در محاسبات این جدول وارد
                                    می‌شود.
                                </p>
                            </div>

                            <span v-if="scoreSheet" class="gss-badge"
                                :class="scoreSheet.hasSubmittedScores ? 'gss-badge-success' : 'gss-badge-warning'">
                                {{ scoreSheet.evaluatorWithSubmittedScoreCount }} داور دارای امتیاز
                            </span>
                        </div>
                    </div>

                    <div class="gss-card-body">
                        <div v-if="scoreSheet?.message" class="gss-alert gss-alert-warning">
                            <i class="bi bi-info-circle"></i>
                            <span>{{ scoreSheet.message }}</span>
                        </div>

                        <div v-if="!scoreSheet" class="admin-empty-state">
                            اطلاعات امتیازها موجود نیست.
                        </div>

                        <template v-else>
                            <div v-for="eventBlock in scoreSheet.events" :key="eventBlock.eventId"
                                class="admin-score-event">
                                <div class="admin-score-event-header">
                                    <h3>{{ eventBlock.eventName }}</h3>
                                    <span>{{ eventBlock.indicators.length }} شاخص</span>
                                </div>

                                <div class="gss-table-scroll">
                                    <table class="table admin-score-table">
                                        <thead>
                                            <tr>
                                                <th class="gss-sticky-right">گروه / تیم</th>
                                                <th v-for="indicator in eventBlock.indicators"
                                                    :key="indicator.indicatorId">
                                                    {{ indicator.indicatorName }}
                                                </th>
                                                <th>مجموع</th>
                                                <th>میانگین</th>
                                                <th>توضیحات ثبت‌شده</th>
                                            </tr>
                                        </thead>

                                        <tbody>
                                            <tr v-for="row in eventBlock.rows" :key="row.teamId">
                                                <td class="gss-sticky-right fw-bold">
                                                    <div class="admin-team-name-with-rank">
                                                        <span>{{ row.teamName }}</span>
                                                        &nbsp;
                                                        <span v-if="getEventTeamRank(eventBlock.eventId, row.teamId)"
                                                              class="admin-rank-badge"
                                                              :class="getRankBadgeClass(getEventTeamRank(eventBlock.eventId, row.teamId))">
                                                            {{ getEventTeamRank(eventBlock.eventId, row.teamId) }}
                                                        </span>
                                                    </div>
                                                </td>

                                                <td v-for="value in row.indicatorValues" :key="value.indicatorId"
                                                    class="admin-score-value-cell">
                                                    <span class="admin-score-value" tabindex="0">
                                                        {{ formatScore(value.averageScore) }}

                                                        <span v-if="value.evaluatorScores.length > 0"
                                                            class="admin-score-tooltip">
                                                            <strong>
                                                                جزئیات داورها
                                                                <small>({{ value.submittedScoreCount }} ثبت)</small>
                                                            </strong>

                                                            <span v-for="item in value.evaluatorScores"
                                                                :key="item.evaluatorProfileId">
                                                                {{ item.evaluatorName }}: {{ item.score }}
                                                            </span>
                                                        </span>
                                                    </span>
                                                </td>

                                                <td class="fw-bold">{{ formatScore(row.total) }}</td>
                                                <td class="fw-bold">{{ formatScore(row.average) }}</td>
                                                <td class="admin-comment-cell">
                                                    {{ row.combinedComments ?? '-' }}
                                                </td>
                                            </tr>
                                        </tbody>
                                    </table>
                                </div>
                            </div>

                            <div class="admin-final-results">
                                <div class="admin-final-results-header">
                                    <div>
                                        <h3>نتیجه نهایی</h3>
                                        <p>امتیاز مجموع از جمع مجموع رویدادها و میانگین از میانگینِ میانگین‌های رویدادها محاسبه می‌شود.</p>
                                    </div>

                                    <span class="gss-badge gss-badge-muted">
                                        {{ finalScoreRows.length }} تیم
                                    </span>
                                </div>

                                <div class="gss-table-scroll">
                                    <table class="table admin-final-score-table">
                                        <thead>
                                            <tr>
                                                <th class="gss-sticky-right">نام تیم</th>
                                                <th>امتیاز مجموع</th>
                                                <th>میانگین</th>
                                            </tr>
                                        </thead>

                                        <tbody>
                                            <tr v-for="row in finalScoreRows" :key="row.teamId">
                                                <td class="gss-sticky-right fw-bold">
                                                    <div class="admin-team-name-with-rank">
                                                        <span>{{ row.teamName }}</span>
                                                        &nbsp;
                                                        <span
                                                            v-if="row.rank"
                                                            class="admin-rank-badge"
                                                            :class="getRankBadgeClass(row.rank)"
                                                        >
                                                            {{ row.rank }}
                                                        </span>
                                                    </div>
                                                </td>

                                                <td class="fw-bold">{{ formatScore(row.total) }}</td>
                                                <td class="fw-bold">{{ formatScore(row.average) }}</td>
                                            </tr>
                                        </tbody>
                                    </table>
                                </div>
                            </div>
                        </template>
                    </div>
                </section>
            </template>
        </div>

        <Teleport to="body">
            <div v-if="isResetConfirmOpen" class="gss-modal-backdrop" role="dialog" aria-modal="true" @click.self="closeResetConfirm">
                <div class="gss-modal">
                    <div class="gss-modal-icon danger">
                        <i class="bi bi-arrow-counterclockwise"></i>
                    </div>

                    <h3 class="gss-modal-title">Reset فرم</h3>

                    <p class="gss-modal-text">
                        آیا مطمئن هستید که می‌خواهید این فرم Reset شود؟
                    </p>

                    <p class="gss-modal-warning-text">
                        با Reset، امتیازها، توضیحات، Syncها، Round قبلی و لینک‌های قبلی ارزیاب‌ها حذف می‌شوند؛ اما ساختار فرم، تیم‌ها، رویدادها و شاخص‌ها باقی می‌مانند.
                    </p>

                    <div class="gss-modal-actions">
                        <button class="btn btn-danger" type="button" :disabled="isResetting" @click="confirmReset">
                            <span v-if="isResetting" class="spinner-border spinner-border-sm ms-1" aria-hidden="true"></span>
                            بله، Reset شود
                        </button>

                        <button class="btn btn-light border" type="button" :disabled="isResetting" @click="closeResetConfirm">
                            انصراف
                        </button>
                    </div>
                </div>
            </div>
        </Teleport>

        <Teleport to="body">
            <div v-if="qrModal" class="gss-modal-backdrop" role="dialog" aria-modal="true" @click.self="closeQrModal">
                <div class="gss-modal gss-qr-modal">
                    <div class="gss-modal-icon info">
                        <i class="bi bi-qr-code"></i>
                    </div>

                    <h3 class="gss-modal-title">QR لینک ارزیابی</h3>

                    <p class="gss-modal-text">
                        {{ qrModal.evaluator.evaluatorName }}
                    </p>

                    <div class="gss-qr-box">
                        <img :src="qrModal.dataUrl" alt="QR Code لینک ارزیابی" />
                    </div>

                    <div class="gss-link-box" dir="ltr">
                        {{ qrModal.evaluator.evaluatorUrl }}
                    </div>

                    <div class="gss-modal-actions">
                        <button class="btn btn-primary" type="button" @click="copyToClipboard(qrModal.evaluator.evaluatorUrl)">
                            کپی آدرس
                        </button>

                        <button class="btn btn-light border" type="button" @click="closeQrModal">
                            بستن
                        </button>
                    </div>
                </div>
            </div>
        </Teleport>

        <Teleport to="body">
            <div v-if="unfinalizeConfirmEvaluator" class="gss-modal-backdrop" role="dialog" aria-modal="true" @click.self="closeUnfinalizeConfirm">
                <div class="gss-modal">
                    <div class="gss-modal-icon warning">
                        <i class="bi bi-unlock"></i>
                    </div>

                    <h3 class="gss-modal-title">غیر نهایی کردن فرم داور</h3>

                    <p class="gss-modal-text">
                        آیا مطمئن هستید که می‌خواهید فرم
                        <strong>{{ unfinalizeConfirmEvaluator.evaluatorName }}</strong>
                        از حالت نهایی خارج شود؟
                    </p>

                    <p class="gss-modal-warning-text">
                        امتیازها و توضیحات قبلی حذف نمی‌شوند، اما داور می‌تواند دوباره فرم را ویرایش و ثبت کند.
                    </p>

                    <div class="gss-modal-actions">
                        <button class="btn btn-warning" type="button"
                            :disabled="isUnfinalizingEvaluatorId === unfinalizeConfirmEvaluator.id"
                            @click="confirmUnfinalizeEvaluator">
                            <span v-if="isUnfinalizingEvaluatorId === unfinalizeConfirmEvaluator.id"
                                class="spinner-border spinner-border-sm ms-1"
                                aria-hidden="true"></span>
                            بله، غیر نهایی شود
                        </button>

                        <button class="btn btn-light border" type="button"
                            :disabled="isUnfinalizingEvaluatorId !== null"
                            @click="closeUnfinalizeConfirm">
                            انصراف
                        </button>
                    </div>
                </div>
            </div>
        </Teleport>
    </main>
</template>