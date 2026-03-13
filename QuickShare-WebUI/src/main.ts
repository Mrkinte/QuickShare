import { createApp } from "vue";
import ElementPlus from "element-plus";
import "element-plus/dist/index.css";
import "element-plus/theme-chalk/dark/css-vars.css";
import App from "./App.vue";
import router from "./router";
// import { addCollection } from "@iconify/vue";
// import { icons } from "@iconify-json/fluent";

const app = createApp(App);

// TODO 全包含导致每次访问都要将所有图标加载，占用带宽。
// addCollection(icons);

app.use(router);
app.use(ElementPlus);
app.mount("#app");
