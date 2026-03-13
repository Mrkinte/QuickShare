<script setup lang="ts">
import { Icon } from "@iconify/vue";
import FileIcon from "./FileIcon.vue";
import { formatFileSize } from "../helpers/CustomHelper.ts";

type FileInfoViewProps = {
  open: boolean;
  name: string;
  extension: string;
  size: number;
  creationTime: string;
  lastModified: string;
};
withDefaults(defineProps<FileInfoViewProps>(), {
  open: false,
  name: "Quickshare.exe",
  extension: "文件夹",
  size: 0,
  creationTime: "2025-12-25 08:00:00",
  lastModified: "2025-12-25 08:00:00",
});
const emit = defineEmits<{ close: [] }>();
</script>

<template>
  <div v-if="open" class="root-container" @click="emit('close')">
    <el-card
      class="file-info-card"
      @click="
        (event: MouseEvent) => {
          event.stopPropagation();
        }
      "
    >
      <template #header>
        <div class="card-header">
          <FileIcon :size="32" :extension="extension" />
          <el-text
            truncated
            size="large"
            type="primary"
            style="max-width: 300px"
          >
            {{ extension === ".folder" ? "文件夹" : "文件" }}属性
          </el-text>
          <el-button @click="emit('close')">
            <el-icon size="20" color="red">
              <Icon icon="fluent:dismiss-24-regular" />
            </el-icon>
          </el-button>
        </div>
      </template>
      <div class="file-info-detail">
        <el-descriptions column="vertical">
          <el-descriptions-item label="文件名称">
            {{ name }}
          </el-descriptions-item>
          <el-descriptions-item label="文件类型">
            {{ extension }}
          </el-descriptions-item>
          <el-descriptions-item label="文件大小">
            {{ formatFileSize(size) }}
          </el-descriptions-item>
          <el-descriptions-item label="创建时间">
            {{ creationTime }}
          </el-descriptions-item>
          <el-descriptions-item label="修改时间">
            {{ lastModified }}
          </el-descriptions-item>
        </el-descriptions>
      </div>
    </el-card>
  </div>
</template>

<style scoped>
.root-container {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  z-index: 10;
  display: flex;
  align-items: center;
  justify-content: center;
}

.file-info-card {
  margin: 16px;
  width: 440px;
  display: flex;
}

.card-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.file-info-detail {
  width: 100%;
  display: flex;
  justify-content: center;
}

:deep(.el-descriptions__body .el-descriptions__table .el-descriptions__cell) {
  line-height: 48px;
}

@media (max-width: 540px) {
  .file-info-card {
    width: 100%;
  }
}
</style>
