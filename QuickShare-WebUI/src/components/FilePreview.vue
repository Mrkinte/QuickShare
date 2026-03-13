<script setup lang="ts">
import { fileType } from "../helpers/FileHelper.ts";
import { computed } from "vue";

interface Props {
  visible: boolean;
  fileUrl: string;
  fileName: string;
  fileExtension: string;
}

const props = defineProps<Props>();
const emit = defineEmits<{
  (e: "update:visible", value: boolean): void;
}>();

const dialogVisible = computed({
  get: () => props.visible,
  set: (value) => emit("update:visible", value),
});

const closePreview = () => {
  dialogVisible.value = false;
};
</script>

<template>
  <el-dialog
    v-model="dialogVisible"
    :title="fileName"
    fullscreen
    @close="closePreview"
  >
    <div class="preview-container">
      <div v-if="fileType(fileExtension) === 'image'" class="image-preview">
        <img :src="fileUrl" :alt="fileName" class="preview-image" />
      </div>

      <div
        v-else-if="fileType(fileExtension) === 'video'"
        class="video-preview"
      >
        <video :src="fileUrl" controls class="preview-video">
          不支持预览该文件格式
        </video>
      </div>

      <div v-else class="unsupported-preview">
        <el-empty description="不支持预览该文件格式" />
      </div>
    </div>
  </el-dialog>
</template>

<style scoped>
.preview-container {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.image-preview {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.preview-image {
  max-width: 90vw;
  max-height: 90vh;
  width: auto;
  height: auto;
  object-fit: contain;
  border-radius: 4px;
  box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
}

.video-preview {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
}

.preview-video {
  max-width: 90vw;
  max-height: 80vh;
  width: auto;
  height: auto;
  border-radius: 4px;
  box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
}

.unsupported-preview {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}
</style>
