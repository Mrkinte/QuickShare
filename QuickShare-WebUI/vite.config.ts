import { defineConfig } from "vite";
import vue from "@vitejs/plugin-vue";
import Icons from "unplugin-icons/vite";
import AutoImport from "unplugin-auto-import/vite";
import IconsResolver from "unplugin-icons/resolver";
import Components from "unplugin-vue-components/vite";
import { ElementPlusResolver } from "unplugin-vue-components/resolvers";

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  return {
    plugins: [
      vue(),
      AutoImport({
        resolvers: [ElementPlusResolver()],
      }),
      Components({
        resolvers: [ElementPlusResolver(), IconsResolver({ prefix: "" })],
      }),
      Icons({
        compiler: "vue3",
      }),
    ],
    server:
      mode === "development"
        ? {
            host: "0.0.0.0",
            proxy: {
              "/api": {
                target: "https://127.0.0.1:53579",
                changeOrigin: true,
                secure: false, // 禁用 SSL验证（测试环境使用）
              },
            },
          }
        : undefined,
    build: {
      outDir: "../QuickShare/Assets/wwwroot",
    },
  };
});
