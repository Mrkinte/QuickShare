import { createRouter, createWebHistory } from "vue-router";
import LoginView from "../views/LoginView.vue";
import TransmitView from "../views/TransmitView.vue";
import axios from "axios";
import ShareView from "../views/ShareView.vue";
import VerifyView from "../views/VerifyView.vue";
import InvalidShareView from "../views/InvalidShareView.vue";
import NotFoundView from "../views/NotFoundView.vue";

const routes = [
  {
    path: "/",
    name: "Login",
    component: LoginView,
  },
  {
    path: "/transmit",
    name: "Transmit",
    component: TransmitView,
    beforeEnter: () => {
      axios.get("/api/transmit/logged").then((response) => {
        if (!response.data.isAuthenticated) {
          router.push("/").then(() => {});
        }
      });
    },
  },
  {
    path: "/:pathMatch(.*)*",
    name: "NotFound",
    component: NotFoundView,
    props: true,
  },
  {
    path: "/verify/:shareId",
    name: "Verify",
    component: VerifyView,
    props: true,
  },
  {
    path: "/share/:shareId",
    name: "Share",
    component: ShareView,
    props: true,
  },
  {
    path: "/invalid-share/:shareId",
    name: "InvalidShare",
    component: InvalidShareView,
    props: true,
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

export default router;
