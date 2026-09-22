import { createRouter, createWebHistory } from "vue-router";
import AdminHomePage from "@/pages/admin/AdminHomePage.vue";
import AdminCourseDetailsPage from "@/pages/admin/AdminCourseDetailsPage.vue";
import EvaluatorPage from "@/pages/evaluator/EvaluatorPage.vue";
import NotFoundPage from "@/pages/NotFoundPage.vue";

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: "/",
      redirect: "/admin",
    },
    {
      path: "/admin",
      name: "admin-home",
      component: AdminHomePage,
    },
    {
      path: "/admin/courses/:courseId",
      name: "admin-course-details",
      component: AdminCourseDetailsPage,
      props: true,
    },
    {
      path: "/evaluate/:token",
      name: "evaluator",
      component: EvaluatorPage,
      props: true,
    },
    {
      path: "/:pathMatch(.*)*",
      name: "not-found",
      component: NotFoundPage,
    },
  ],
});
