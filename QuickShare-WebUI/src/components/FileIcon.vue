<script setup lang="ts">
import { fileType } from "../helpers/FileHelper.ts";

type FileIconProps = {
  size: number;
  extension: string;
};
withDefaults(defineProps<FileIconProps>(), {
  size: 48,
  extension: "",
});
</script>

<template>
  <!-- 文件夹图标 -->
  <el-icon
    v-if="fileType(extension) === 'folder'"
    class="folder-icon"
    :size="size"
  >
    <FluentFolderOpen24Filled />
  </el-icon>

  <!-- 图片图标 -->
  <el-icon
    v-else-if="fileType(extension) === 'image'"
    class="image-icon"
    :size="size"
  >
    <FluentImage24Regular />
  </el-icon>

  <!-- 视频图标 -->
  <el-icon
    v-else-if="fileType(extension) === 'video'"
    class="video-icon"
    :size="size"
  >
    <FluentVideoClip24Filled />
  </el-icon>

  <!-- 音频图标 -->
  <el-icon
    v-else-if="fileType(extension) === 'audio'"
    class="audio-icon"
    :size="size"
  >
    <FluentMusicNote2Play20Filled />
  </el-icon>

  <!-- 文档图标 -->
  <el-icon
    v-else-if="fileType(extension) === 'document'"
    class="document-icon"
    :size="size"
  >
    <FluentDocumentTable24Filled />
  </el-icon>

  <!-- 压缩文件图标 -->
  <el-icon
    v-else-if="fileType(extension) === 'compressed'"
    class="compressed-icon"
    :size="size"
  >
    <FluentFolderZip24Filled />
  </el-icon>

  <!-- 默认文件图标 -->
  <el-icon v-else class="default-icon" :size="size">
    <FluentDocument24Filled />
  </el-icon>
</template>

<style scoped>
/* 不同类型图标的颜色 */
.folder-icon {
  color: #ffc53d;
}

.image-icon {
  color: #2979c9;
}

.video-icon {
  color: #ff0133;
}

.document-icon {
  color: #107c41;
}

.audio-icon {
  color: #16bd74;
}

.compressed-icon {
  color: #fa8c16;
}

.default-icon {
  color: #8c8c8c;
}
</style>
