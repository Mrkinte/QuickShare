<script setup lang="ts">
import { onMounted, onUnmounted, ref } from "vue";
import { type FileProps } from "../helpers/FileHelper.ts";
import FileIcon from "./FileIcon.vue";

/**
 * 组件属性。
 */
const props = withDefaults(defineProps<FileProps>(), {
  name: "undefined",
  fileId: undefined,
  extension: "",
});

/**
 * 定义组件事件。
 */
const emit = defineEmits<{
  click: [FileProps];
  "double-click": [FileProps];
  "open-contextmenu": [MouseEvent, FileProps | null];
  checked: [FileProps, boolean];
}>();

const selected = ref(false);
const focused = ref(false);
const checkBoxVisible = ref(false);

const handleClick = () => {
  focused.value = true;
  emit("click", props);
};

const handleDoubleClick = () => {
  emit("double-click", props);
};

const handleContextMenuPrevent = (event: MouseEvent) => {
  emit("open-contextmenu", event, props);
};

const handleCheckBoxChange = () => {
  selected.value = !selected.value;
  emit("checked", props, selected.value);
};

const handleMouseLeave = () => {
  if (!selected.value) {
    checkBoxVisible.value = false;
  }
};

const cancelFocused = () => {
  focused.value = false;
};

onMounted(() => {
  document.addEventListener("mousedown", cancelFocused);
  window.addEventListener("resize", cancelFocused);
});

onUnmounted(() => {
  document.removeEventListener("mousedown", cancelFocused);
  window.removeEventListener("resize", cancelFocused);
});
</script>

<template>
  <div
    class="root-container"
    :class="{
      'root-container--selected': focused,
    }"
    @mouseenter="checkBoxVisible = true"
    @mouseleave="handleMouseLeave"
    @click="handleClick"
    @dblclick="handleDoubleClick"
    @contextmenu.stop.prevent="handleContextMenuPrevent"
  >
    <el-checkbox
      v-if="checkBoxVisible"
      v-model:checked="selected"
      class="checkbox"
      @change="handleCheckBoxChange"
      :disabled="extension === '.folder'"
    />
    <FileIcon :extension="extension" :size="48" />
    <el-text truncated>{{ name }}</el-text>
  </div>
</template>

<style scoped>
@import "context-menu.css";

.root-container {
  width: 96px;
  height: 96px;
  cursor: pointer;
  position: relative;
  border-radius: 10px;
  align-items: center;
  display: inline-flex;
  flex-direction: column;
  justify-content: center;
}

.root-container:hover {
  background-color: rgba(64, 158, 255, 0.08);
}

.root-container--selected {
  background-color: rgba(64, 158, 255, 0.12);
  outline: 1px solid rgba(64, 158, 255, 0.3);
}

.checkbox {
  top: -2px;
  left: 6px;
  position: absolute;
}
</style>
