<script setup lang="ts">
import type { FileProps } from "../helpers/FileHelper.ts";

/**
 * 组件属性。
 */
type MenuBarProps = {
  fileProps: FileProps;
  visible: boolean;
  openButtonDisabled: boolean;
  downloadButtonDisabled: boolean;
  viewPropsButtonDisabled: boolean;
};
withDefaults(defineProps<MenuBarProps>(), {
  name: "undefined",
  fileId: undefined,
  extension: "",
  visible: false,
  openButtonDisabled: false,
  downloadButtonDisabled: false,
  viewPropsButtonDisabled: false,
});

/**
 * 定义组件事件。
 */
const emit = defineEmits<{
  "open-click": [FileProps];
  "download-click": [FileProps];
  "view-props-click": [FileProps];
}>();
</script>

<template>
  <div v-if="visible" class="root-container">
    <el-card class="menu-card">
      <div class="bottom-menu-bar">
        <el-button
          size="large"
          :disabled="openButtonDisabled"
          @click="emit('open-click', fileProps)"
        >
          <div class="menu-bar-button">
            <el-icon size="20">
              <FluentDocumentAdd24Filled />
            </el-icon>
            打开
          </div></el-button
        >
        <el-button
          size="large"
          :disabled="downloadButtonDisabled"
          @click="emit('download-click', fileProps)"
        >
          <div class="menu-bar-button">
            <el-icon size="20">
              <FluentCloudDownload24Filled />
            </el-icon>
            下载
          </div></el-button
        >
        <el-button
          size="large"
          :disabled="viewPropsButtonDisabled"
          @click="emit('view-props-click', fileProps)"
        >
          <div class="menu-bar-button">
            <el-icon size="20">
              <FluentInfo24Filled />
            </el-icon>
            属性
          </div></el-button
        >
      </div>
    </el-card>
  </div>
</template>

<style scoped>
.root-container {
  position: fixed;
  bottom: 0;
  left: 0;
  width: 100%;
  z-index: 5;
  display: flex;
  align-items: end;
  justify-content: center;
}

.menu-card {
  margin-bottom: 20px;
}

.bottom-menu {
  display: flex;
}

.menu-bar-button {
  gap: 8px;
  display: flex;
  flex-direction: row;
  align-items: center;
}

@media (max-width: 540px) {
}
</style>
