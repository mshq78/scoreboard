<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { isValidJalaaliDate, toGregorian, toJalaali } from 'jalaali-js'
import { createCourse } from '@/api/adminApi'
import { getApiErrorMessage } from '@/api/http'

const emit = defineEmits<{
    created: []
}>()

const organizerCompanyName = ref('')
const formCreatedJalaliDate = ref('')
const teamCount = ref(1)
const teamNames = ref<string[]>([''])
const excelFile = ref<File | null>(null)

const isSubmitting = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const todayJalali = computed(() => {
    const today = new Date()
    const jalali = toJalaali(
        today.getFullYear(),
        today.getMonth() + 1,
        today.getDate(),
    )

    return `${jalali.jy}/${String(jalali.jm).padStart(2, '0')}/${String(jalali.jd).padStart(2, '0')}`
})

const formCreatedGregorianDate = computed(() => {
    const parsed = parseJalaliDate(formCreatedJalaliDate.value)

    if (!parsed) {
        return null
    }

    const gregorian = toGregorian(parsed.year, parsed.month, parsed.day)

    return `${gregorian.gy}-${String(gregorian.gm).padStart(2, '0')}-${String(gregorian.gd).padStart(2, '0')}`
})

const selectedFileName = computed(() => {
    return excelFile.value?.name ?? 'فایلی انتخاب نشده است'
})

const canSubmit = computed(() => {
    return (
        organizerCompanyName.value.trim().length > 0 &&
        formCreatedGregorianDate.value !== null &&
        teamCount.value > 0 &&
        teamNames.value.length === teamCount.value &&
        teamNames.value.every((name) => name.trim().length > 0) &&
        excelFile.value !== null &&
        !isSubmitting.value
    )
})

watch(teamCount, (newValue) => {
    const normalizedCount = Number(newValue)

    if (!Number.isFinite(normalizedCount) || normalizedCount < 1) {
        teamCount.value = 1
        return
    }

    if (normalizedCount > 100) {
        teamCount.value = 100
        return
    }

    const currentNames = [...teamNames.value]

    if (normalizedCount > currentNames.length) {
        while (currentNames.length < normalizedCount) {
            currentNames.push('')
        }
    } else if (normalizedCount < currentNames.length) {
        currentNames.length = normalizedCount
    }

    teamNames.value = currentNames
})

function handleFileChange(event: Event) {
    const input = event.target as HTMLInputElement
    const file = input.files?.[0] ?? null

    excelFile.value = file
}

function resetForm() {
    organizerCompanyName.value = ''
    formCreatedJalaliDate.value = ''
    teamCount.value = 1
    teamNames.value = ['']
    excelFile.value = null
    errorMessage.value = ''
    successMessage.value = ''

    const fileInput = document.getElementById('course-excel-file') as HTMLInputElement | null

    if (fileInput) {
        fileInput.value = ''
    }
}

async function handleSubmit() {
    errorMessage.value = ''
    successMessage.value = ''

    if (!canSubmit.value || excelFile.value === null) {
        errorMessage.value = 'لطفاً همه اطلاعات لازم را کامل و صحیح وارد کنید.'
        return
    }

    const normalizedTeamNames = teamNames.value.map((name) => name.trim())

    const duplicateTeamName = normalizedTeamNames.find((name, index, array) => {
        return array.findIndex((item) => item.toLowerCase() === name.toLowerCase()) !== index
    })

    if (duplicateTeamName) {
        errorMessage.value = `نام تیم تکراری است: ${duplicateTeamName}`
        return
    }

    if (!formCreatedGregorianDate.value) {
        errorMessage.value = 'تاریخ ایجاد فرم باید به‌صورت شمسی و معتبر وارد شود. مثال: 1403/03/21'
        return
    }

    isSubmitting.value = true

    try {
        await createCourse({
            organizerCompanyName: organizerCompanyName.value.trim(),
            holdingDate: formCreatedGregorianDate.value,
            teamCount: teamCount.value,
            teamNames: normalizedTeamNames,
            excelFile: excelFile.value,
        })

        successMessage.value = 'دوره با موفقیت ثبت شد.'
        resetForm()
        emit('created')
    } catch (error) {
        errorMessage.value = getApiErrorMessage(error)
    } finally {
        isSubmitting.value = false
    }
}

function normalizeJalaliDateInput() {
    formCreatedJalaliDate.value = toEnglishDigits(formCreatedJalaliDate.value)
        .replace(/-/g, '/')
        .replace(/\./g, '/')
        .replace(/[^\d/]/g, '')
}

function parseJalaliDate(value: string): { year: number; month: number; day: number } | null {
    const normalized = toEnglishDigits(value.trim())
        .replace(/-/g, '/')
        .replace(/\./g, '/')

    const parts = normalized.split('/')

    if (parts.length !== 3) {
        return null
    }

    const year = Number(parts[0])
    const month = Number(parts[1])
    const day = Number(parts[2])

    if (
        !Number.isInteger(year) ||
        !Number.isInteger(month) ||
        !Number.isInteger(day)
    ) {
        return null
    }

    if (!isValidJalaaliDate(year, month, day)) {
        return null
    }

    return {
        year,
        month,
        day,
    }
}

function toEnglishDigits(value: string): string {
    return value
        .replace(/[۰-۹]/g, (digit) => String('۰۱۲۳۴۵۶۷۸۹'.indexOf(digit)))
        .replace(/[٠-٩]/g, (digit) => String('٠١٢٣٤٥٦٧٨٩'.indexOf(digit)))
}

</script>

<template>
    <section class="gss-card admin-create-card">
        <div class="gss-card-header">
            <div class="d-flex align-items-start justify-content-between gap-3">
                <div>
                    <h2 class="gss-card-title">
                        <i class="bi bi-plus-circle ms-1"></i>
                        ایجاد دوره جدید
                    </h2>
                    <p class="gss-card-subtitle">
                        اطلاعات دوره، تیم‌ها و فایل Excel رویدادها و شاخص‌ها را وارد کنید.
                    </p>
                </div>

                <span class="gss-badge gss-badge-muted d-none d-sm-inline-flex">
                    Excel .xlsx
                </span>
            </div>
        </div>

        <div class="gss-card-body">
            <div v-if="errorMessage" class="gss-alert gss-alert-danger">
                <i class="bi bi-exclamation-triangle"></i>
                <span>{{ errorMessage }}</span>
            </div>

            <div v-if="successMessage" class="gss-alert gss-alert-success">
                <i class="bi bi-check-circle"></i>
                <span>{{ successMessage }}</span>
            </div>

            <form @submit.prevent="handleSubmit">
                <div class="row g-3">
                    <div class="col-12 col-lg-5">
                        <label class="form-label">نام شرکت برگزارکننده</label>
                        <input v-model="organizerCompanyName" class="form-control" placeholder="مثلاً شرکت تست"
                            autocomplete="organization" />
                    </div>

                    <div class="col-12 col-sm-6 col-lg-3">
                        <label class="form-label">تاریخ ایجاد فرم</label>
                        <input v-model="formCreatedJalaliDate" class="form-control" type="text" inputmode="numeric"
                            dir="ltr" :placeholder="todayJalali" @input="normalizeJalaliDateInput" />
                        <div class="form-text">
                            تاریخ را شمسی وارد کنید. مثال: {{ todayJalali }}
                        </div>
                    </div>

                    <div class="col-12 col-sm-6 col-lg-2">
                        <label class="form-label">تعداد تیم‌ها</label>
                        <input v-model.number="teamCount" class="form-control" type="number" inputmode="numeric" min="1"
                            max="100" />
                    </div>

                    <div class="col-12 col-lg-2">
                        <label class="form-label">فایل Excel</label>
                        <input id="course-excel-file" class="form-control" type="file" accept=".xlsx"
                            @change="handleFileChange" />
                    </div>
                </div>

                <div class="mt-2 small text-muted text-truncate">
                    <i class="bi bi-paperclip ms-1"></i>
                    {{ selectedFileName }}
                </div>

                <div class="admin-team-box mt-3">
                    <div class="d-flex align-items-center justify-content-between gap-2 mb-3">
                        <h3 class="admin-section-title mb-0">
                            نام تیم‌ها
                        </h3>

                        <span class="gss-badge">
                            {{ teamCount }} تیم
                        </span>
                    </div>

                    <div class="row g-2 g-md-3">
                        <div v-for="(_, index) in teamNames" :key="index" class="col-12 col-sm-6 col-lg-4 col-xl-3">
                            <label class="form-label">
                                تیم {{ index + 1 }}
                            </label>
                            <input v-model="teamNames[index]" class="form-control" :placeholder="`نام تیم ${index + 1}`"
                                autocomplete="off" />
                        </div>
                    </div>
                </div>

                <!--<div class="gss-alert gss-alert-info mt-3 mb-0">
                    <i class="bi bi-info-circle"></i>
                    <div>
                        <strong>فرمت Excel:</strong>
                        ستون اول نام رویداد است و ستون‌های بعدی شاخص‌های همان رویداد هستند.
                        ردیف اول Header است.
                        <div class="ltr-sample mt-1">
                            Event Name | Indicator 1 | Indicator 2
                        </div>
                    </div>
                </div>-->

                <div class="d-grid d-sm-flex gap-2 mt-3">
                    <button class="btn btn-primary" type="submit" :disabled="!canSubmit">
                        <span v-if="isSubmitting" class="spinner-border spinner-border-sm ms-1"
                            aria-hidden="true"></span>
                        {{ isSubmitting ? 'در حال ثبت...' : 'ثبت دوره' }}
                    </button>

                    <button class="btn btn-light border" type="button" :disabled="isSubmitting" @click="resetForm">
                        پاک کردن فرم
                    </button>
                </div>
            </form>
        </div>
    </section>
</template>