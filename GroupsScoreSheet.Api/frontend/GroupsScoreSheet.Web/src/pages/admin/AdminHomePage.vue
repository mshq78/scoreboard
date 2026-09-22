<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { RouterLink } from 'vue-router'
import CreateCourseForm from '@/components/admin/CreateCourseForm.vue'
import { deleteCourse, getCourses } from '@/api/adminApi'
import { getAdminToken, getApiErrorMessage, setAdminToken } from '@/api/http'
import type { CourseListItemDto } from '@/types/admin'

const adminToken = ref(getAdminToken())
const courses = ref<CourseListItemDto[]>([])
const isLoading = ref(false)
const errorMessage = ref('')

const coursePendingDelete = ref<CourseListItemDto | null>(null)
const isDeletingCourse = ref(false)

const hasToken = computed(() => adminToken.value.trim().length > 0)

const totalCourses = computed(() => courses.value.length)

const totalEvaluators = computed(() => {
    return courses.value.reduce((sum, course) => sum + course.evaluatorCount, 0)
})

const totalFinalized = computed(() => {
    return courses.value.reduce((sum, course) => sum + course.finalizedEvaluatorCount, 0)
})

function saveToken() {
    setAdminToken(adminToken.value)
    loadCourses()
}

async function loadCourses() {
    if (!hasToken.value) {
        errorMessage.value = 'ابتدا Admin Token را وارد کنید.'
        return
    }

    isLoading.value = true
    errorMessage.value = ''

    try {
        courses.value = await getCourses()
    } catch (error) {
        errorMessage.value = getApiErrorMessage(error)
    } finally {
        isLoading.value = false
    }
}

function formatDate(value: string) {
    return new Date(value).toLocaleDateString('fa-IR')
}

function openDeleteCourseModal(course: CourseListItemDto) {
    coursePendingDelete.value = course
}

function closeDeleteCourseModal() {
    if (isDeletingCourse.value) {
        return
    }

    coursePendingDelete.value = null
}

async function confirmDeleteCourse() {
    if (!coursePendingDelete.value) {
        return
    }

    isDeletingCourse.value = true
    errorMessage.value = ''

    try {
        await deleteCourse(coursePendingDelete.value.id)
        coursePendingDelete.value = null
        await loadCourses()
    } catch (error) {
        errorMessage.value = getApiErrorMessage(error)
    } finally {
        isDeletingCourse.value = false
    }
}

onMounted(() => {
    if (hasToken.value) {
        loadCourses()
    }
})
</script>

<template>
    <main class="gss-page">
        <div class="gss-container">
            <header class="gss-header">
                <div>
                    <h1 class="gss-title">
                        مدیریت ارزیابی گروه‌ها
                    </h1>
                    <p class="gss-subtitle">
                        ساخت دوره، مدیریت ارزیاب‌ها، مشاهده امتیازها و دریافت خروجی Excel
                    </p>
                </div>

                <div class="d-flex flex-wrap gap-2">
                    <span class="gss-badge gss-badge-muted">
                        <i class="bi bi-phone"></i>
                        Mobile First
                    </span>
                    <span class="gss-badge gss-badge-success">
                        <i class="bi bi-wifi-off"></i>
                        Offline Ready
                    </span>
                </div>
            </header>

            <section class="gss-card mb-3">
                <div class="gss-card-body">
                    <div class="row g-2 align-items-end">
                        <div class="col-12 col-md">
                            <label class="form-label">Admin Token</label>
                            <input v-model="adminToken" class="form-control" placeholder="مثلاً dev-admin-token"
                                type="password" autocomplete="current-password" @keyup.enter="saveToken" />
                        </div>

                        <div class="col-12 col-md-auto">
                            <button class="btn btn-primary w-100" type="button" :disabled="isLoading"
                                @click="saveToken">
                                <span v-if="isLoading" class="spinner-border spinner-border-sm ms-1"
                                    aria-hidden="true"></span>
                                ذخیره و دریافت دوره‌ها
                            </button>
                        </div>
                    </div>
                </div>
            </section>

            <section v-if="hasToken" class="admin-stats-grid mb-3">
                <div class="admin-stat-card">
                    <div class="admin-stat-icon">
                        <i class="bi bi-collection"></i>
                    </div>
                    <div>
                        <div class="admin-stat-value">{{ totalCourses }}</div>
                        <div class="admin-stat-label">دوره</div>
                    </div>
                </div>

                <div class="admin-stat-card">
                    <div class="admin-stat-icon">
                        <i class="bi bi-person-check"></i>
                    </div>
                    <div>
                        <div class="admin-stat-value">{{ totalEvaluators }}</div>
                        <div class="admin-stat-label">ارزیاب</div>
                    </div>
                </div>

                <div class="admin-stat-card">
                    <div class="admin-stat-icon">
                        <i class="bi bi-check2-circle"></i>
                    </div>
                    <div>
                        <div class="admin-stat-value">{{ totalFinalized }}</div>
                        <div class="admin-stat-label">نهایی‌شده</div>
                    </div>
                </div>
            </section>

            <CreateCourseForm v-if="hasToken" class="mb-3" @created="loadCourses" />

            <section class="gss-card">
                <div class="gss-card-header">
                    <div class="d-flex align-items-start justify-content-between gap-3">
                        <div>
                            <h2 class="gss-card-title">
                                <i class="bi bi-list-task ms-1"></i>
                                فرم ها
                            </h2>
                            <p class="gss-card-subtitle">
                                لیست قالب فرم‌های آماده‌شده </p>
                        </div>

                        <button class="btn btn-light border btn-sm" type="button" :disabled="isLoading || !hasToken"
                            @click="loadCourses">
                            <i class="bi bi-arrow-clockwise ms-1"></i>
                            بروزرسانی
                        </button>
                    </div>
                </div>

                <div class="gss-card-body">
                    <div v-if="errorMessage" class="gss-alert gss-alert-danger">
                        <i class="bi bi-exclamation-triangle"></i>
                        <span>{{ errorMessage }}</span>
                    </div>

                    <div v-if="isLoading" class="admin-empty-state">
                        <div class="spinner-border text-primary mb-3" role="status"></div>
                        <div>در حال دریافت اطلاعات...</div>
                    </div>

                    <div v-else-if="courses.length === 0" class="admin-empty-state">
                        <i class="bi bi-inbox admin-empty-icon"></i>
                        <div class="fw-bold">هنوز دوره‌ای ثبت نشده است.</div>
                        <div class="text-muted small mt-1">
                            از فرم بالا اولین دوره را ایجاد کنید.
                        </div>
                    </div>

                    <template v-else>
                        <!-- Mobile cards -->
                        <div class="d-md-none admin-course-list-mobile">
                            <article v-for="course in courses" :key="course.id" class="admin-course-card">
                                <div class="d-flex align-items-start justify-content-between gap-2">
                                    <div>
                                        <h3 class="admin-course-title">
                                            {{ course.organizerCompanyName }}
                                        </h3>
                                        <div class="admin-course-meta">
                                            <i class="bi bi-calendar3 ms-1"></i>
                                            {{ formatDate(course.holdingDate) }}
                                        </div>
                                    </div>

                                    <span class="gss-badge">
                                        Round {{ course.activeRoundNumber }}
                                    </span>
                                </div>

                                <div class="admin-course-kpis">
                                    <div>
                                        <strong>{{ course.teamCount }}</strong>
                                        <span>تیم</span>
                                    </div>
                                    <div>
                                        <strong>{{ course.eventCount }}</strong>
                                        <span>رویداد</span>
                                    </div>
                                    <div>
                                        <strong>{{ course.indicatorCount }}</strong>
                                        <span>شاخص</span>
                                    </div>
                                    <div>
                                        <strong>{{ course.finalizedEvaluatorCount }}/{{ course.evaluatorCount
                                            }}</strong>
                                        <span>نهایی</span>
                                    </div>
                                </div>

                                <div class="d-grid gap-2 mt-3">
                                    <RouterLink class="btn btn-primary" :to="`/admin/courses/${course.id}`">
                                        مشاهده جزئیات
                                    </RouterLink>

                                    <button class="btn btn-outline-danger" type="button"
                                        @click="openDeleteCourseModal(course)">
                                        حذف فرم
                                    </button>
                                </div>
                            </article>
                        </div>

                        <!-- Desktop / tablet table -->
                        <div class="d-none d-md-block gss-table-scroll">
                            <table class="table table-hover align-middle">
                                <thead>
                                    <tr>
                                        <th>شرکت</th>
                                        <th>تاریخ</th>
                                        <th>تیم‌ها</th>
                                        <th>رویدادها</th>
                                        <th>شاخص‌ها</th>
                                        <th>Round</th>
                                        <th>ارزیاب‌ها</th>
                                        <th>نهایی‌شده</th>
                                        <th>وضعیت</th>
                                        <th class="text-center">عملیات</th>
                                    </tr>
                                </thead>

                                <tbody>
                                    <tr v-for="course in courses" :key="course.id">
                                        <td class="fw-bold">{{ course.organizerCompanyName }}</td>
                                        <td>{{ formatDate(course.holdingDate) }}</td>
                                        <td>{{ course.teamCount }}</td>
                                        <td>{{ course.eventCount }}</td>
                                        <td>{{ course.indicatorCount }}</td>
                                        <td>
                                            <span class="gss-badge">Round {{ course.activeRoundNumber }}</span>
                                        </td>
                                        <td>{{ course.evaluatorCount }}</td>
                                        <td>{{ course.finalizedEvaluatorCount }}</td>
                                        <td>
                                            <span class="gss-badge gss-badge-success">{{ course.status }}</span>
                                        </td>
                                        <td class="text-center">
                                            <div class="d-flex justify-content-center gap-2">
                                                <RouterLink class="btn btn-sm btn-outline-primary"
                                                    :to="`/admin/courses/${course.id}`">
                                                    جزئیات
                                                </RouterLink>

                                                <button class="btn btn-sm btn-outline-danger" type="button"
                                                    @click="openDeleteCourseModal(course)">
                                                    حذف
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
        </div>

    </main>

    <Teleport to="body">
        <div v-if="coursePendingDelete" class="delete-course-modal-backdrop" role="dialog" aria-modal="true"
            @click.self="closeDeleteCourseModal">
            <div class="delete-course-modal">
                <div class="delete-course-modal-icon">
                    <i class="bi bi-trash3"></i>
                </div>

                <h3 class="delete-course-modal-title">
                    حذف کامل فرم
                </h3>

                <p class="delete-course-modal-text">
                    آیا مطمئن هستید که می‌خواهید فرم
                    <strong>{{ coursePendingDelete.organizerCompanyName }}</strong>
                    را حذف کنید؟
                </p>

                <div class="delete-course-warning">
                    <i class="bi bi-exclamation-triangle ms-1"></i>
                    همه اطلاعات این فرم شامل تیم‌ها، رویدادها، شاخص‌ها، داورها، امتیازها، توضیحات و خروجی‌های Sync حذف
                    می‌شود.
                </div>

                <div class="delete-course-modal-actions">
                    <button class="btn btn-danger" type="button" :disabled="isDeletingCourse"
                        @click="confirmDeleteCourse">
                        <span v-if="isDeletingCourse" class="spinner-border spinner-border-sm ms-1"
                            aria-hidden="true"></span>
                        بله، حذف شود
                    </button>

                    <button class="btn btn-light border" type="button" :disabled="isDeletingCourse"
                        @click="closeDeleteCourseModal">
                        انصراف
                    </button>
                </div>
            </div>
        </div>
    </Teleport>

</template>