<script setup lang="ts">
import axios from "axios";
import { Vue3Menus } from "vue3-menus";
import FileComponent from "../components/FileComponent.vue";
import { ArrowRight, Search } from "@element-plus/icons-vue";
import { computed, onMounted, ref, shallowRef } from "vue";
import {
  downloadFile,
  formatFileSize,
  getApiBaseUrl,
  isTouchDevice,
  sleep,
} from "../helpers/CustomHelper.ts";
import {
  ElNotification,
  ElMessageBox,
  type UploadInstance,
} from "element-plus";
import { type FileProps, fileType } from "../helpers/FileHelper.ts";
import FileIcon from "../components/FileIcon.vue";
import FileInfoView from "../components/FileInfoView.vue";
import BottomMenuBar from "../components/BottomMenuBar.vue";

const canTouch = ref(false);
const isListView = ref(false);
const uploadRef = ref<UploadInstance>();
const uploadMaxSize = ref(0);

const multipleSelection = ref<FileProps[]>([]);
const currentSelection = ref<FileProps>({ name: "", fileId: 0, extension: "" });

const imageIndex = ref(0);
const imageSrcList = ref<string[]>([]);
const showImagePreview = ref(false);

//region 筛选文件
const showSearchBar = ref(false);
const files = ref<FileProps[]>([]);
const searchText = ref("");
const filteredFiles = computed(() => {
  let result = [...files.value];

  if (searchText.value) {
    result = result.filter((file) =>
      file.name.toLowerCase().includes(searchText.value.toLowerCase()),
    );
  }
  return [...result];
});
//endregion

//region API相关方法
const getFiles = async (path: string) => {
  searchText.value = "";
  const formData = new FormData();
  formData.append("path", path);
  await axios
    .post(`/api/transmit/files`, formData)
    .then((response) => {
      if (
        !response.headers["content-type"].includes("application/json") ||
        response.status !== 200
      ) {
        ElNotification.error({ title: "错误", message: "获取文件列表失败。" });
        return [];
      }
      files.value = response.data;
      currentPath.value = path;
    })
    .catch(() => {
      ElNotification.error({ title: "错误", message: "获取文件列表失败。" });
    });
};

const createFolder = async (path: string) => {
  const formData = new FormData();
  formData.append("path", path);
  await axios
    .post(`/api/transmit/createFolder`, formData)
    .then((response) => {
      if (response.data.message === "Successful") {
        ElNotification.success({ title: "成功", message: "新建文件夹成功。" });
        getFiles(currentPath.value);
      } else {
        ElNotification.error({ title: "错误", message: "新建文件夹失败。" });
      }
    })
    .catch((error) => {
      if (error.response.data.error === "Existed") {
        ElNotification.error({
          title: "错误",
          message: "创建失败，文件夹已存在。",
        });
      } else {
        ElNotification.error({ title: "错误", message: "新建文件夹失败。" });
      }
    });
};

/**
 * 获取文件信息
 */
const openFileInfoView = ref(false);
interface FileInfo {
  name: string;
  size: number;
  extension: string;
  creationTime: string;
  lastModified: string;
}
const fileInfo = ref<FileInfo>({
  name: "",
  extension: "",
  size: 0,
  creationTime: "",
  lastModified: "",
});
const getFileInfo = async (path: string) => {
  const formData = new FormData();
  formData.append("path", path);
  await axios
    .post(`/api/transmit/fileInfo`, formData)
    .then((response) => {
      if (
        !response.headers["content-type"].includes("application/json") ||
        response.status !== 200
      ) {
        ElNotification.error({
          title: "错误",
          message: "获取文件详细信息失败。",
        });
        return null;
      }
      fileInfo.value = response.data;
      openFileInfoView.value = true;
    })
    .catch(() => {
      ElNotification.error({
        title: "错误",
        message: "获取文件详细信息失败。",
      });
    });
};

const getUploadParams = async () => {
  await axios.get(`/api/transmit/parameter`).then((response) => {
    if (
      !response.headers["content-type"].includes("application/json") ||
      response.status !== 200
    ) {
      ElNotification.error({
        title: "错误",
        message: "获取上传参数失败。",
      });
      return;
    }
    uploadMaxSize.value = response.data.maxFileSize * 1024 * 1024;
  });
};
//endregion

//region 事件处理相关方法
const handleClick = (fileProps: FileProps) => {
  if (canTouch.value) {
    handleDoubleClick(fileProps);
  }
};

const handleDoubleClick = (fileProps: FileProps) => {
  const normalizedPath = currentPath.value.endsWith("/")
    ? currentPath.value
    : `${currentPath.value}/`;
  if (fileProps.extension === ".folder") {
    getFiles(normalizedPath + fileProps.name + "/");
  } else if (fileType(fileProps.extension) === "image") {
    let index = 0;
    imageSrcList.value = [];
    files.value.forEach((file) => {
      if (fileType(file.extension) === "image") {
        if (file.name === fileProps.name) {
          imageIndex.value = index;
        }
        const fullSrc =
          getApiBaseUrl() +
          `/api/transmit/download${normalizedPath}${file.name}`;
        imageSrcList.value.push(fullSrc);
        index += 1;
      }
    });
    showImagePreview.value = true;
  } else {
    ElNotification.info({
      title: "提示",
      message: "仅支持预览图片文件，暂不支持该文件格式的预览。",
    });
  }
};

const handleDownload = async (fileProps: FileProps) => {
  const normalizedPath = currentPath.value.endsWith("/")
    ? currentPath.value
    : `${currentPath.value}/`;
  if (multipleSelection.value.length <= 1) {
    downloadFile("/api/transmit/download" + normalizedPath + fileProps.name);
  } else {
    for (const file of multipleSelection.value) {
      downloadFile("/api/transmit/download" + normalizedPath + file.name);
      await sleep(500); // 延时 500ms
    }
  }
};

const handleFileInfo = (fileProps: FileProps) => {
  const normalizedPath = currentPath.value.endsWith("/")
    ? currentPath.value
    : `${currentPath.value}/`;
  getFileInfo(normalizedPath + fileProps.name);
};

const handleUpload = () => {
  if (!uploadRef.value) {
    return;
  }
  const uploadDiv = uploadRef.value.$el.querySelector(
    ".el-upload.el-upload--text",
  );
  if (uploadDiv) {
    uploadDiv.click();
  }
};

const handleCreateFolder = async () => {
  ElMessageBox.prompt("请输入文件夹名称", "新建文件夹", {
    confirmButtonText: "确认",
    cancelButtonText: "取消",
    inputPattern: /^[^\\\/:*?"<>|]*$/,
    inputErrorMessage: '文件夹名称不能包含以下字符：\\ / : * ? " < > |',
    inputPlaceholder: "请输入文件夹名称",
    inputValidator: (value) => {
      if (!value || value.trim() === "") {
        return "文件夹名称不能为空";
      }
      if (value.length > 64) {
        return "文件夹名称不能超过64个字符";
      }
      return true;
    },
  })
    .then(({ value }) => {
      // 去除首尾空格
      const folderName = value.trim();
      const normalizedPath = currentPath.value.endsWith("/")
        ? currentPath.value
        : `${currentPath.value}/`;
      createFolder(normalizedPath + folderName);
    })
    .catch(() => {});
};

const handleViewMethod = (method: boolean) => {
  isListView.value = method;
  multipleSelection.value = [];
  localStorage.setItem("isListView", method.toString());
};

const handleFileComponentChecked = (fileProps: FileProps, checked: boolean) => {
  if (checked) {
    multipleSelection.value.push(fileProps);
  } else {
    if (multipleSelection.value.includes(fileProps)) {
      multipleSelection.value.splice(
        multipleSelection.value.indexOf(fileProps),
        1,
      );
    }
  }
};
//endregion

//region 路径导航相关变量、方法等。
const currentPath = ref("/");
const lastPath = ref("/");
interface BreadcrumbItem {
  label: string;
  path: string;
}

defineExpose({
  currentPath,
});

const breadcrumbs = computed<BreadcrumbItem[]>(() => {
  const parts = currentPath.value.split("/").filter((part) => part !== "");

  if (parts.length === 0) {
    return [{ label: "文件", path: "/" }];
  }
  const items: BreadcrumbItem[] = [{ label: "文件", path: "/" }];
  let count = 0;
  let accumulatedPath = "";
  parts.forEach((part) => {
    accumulatedPath += `/${part}`;
    items.push({ label: part, path: accumulatedPath });
    count += 1;
    if (count === parts.length - 1) {
      lastPath.value = accumulatedPath;
    } else if (parts.length === 1) {
      lastPath.value = "/";
    }
  });
  return items;
});

const navigateTo = (path: string) => {
  getFiles(path);
};
//endregion

//region 右键菜单相关变量、方法等。
const menuIsOpen = ref(false);
const menuEvent = ref<MouseEvent | undefined>({
  clientX: 0,
  clientY: 0,
} as MouseEvent);
const menuItems = shallowRef<any[]>([]);

const fileMenuItems = (fileProps: FileProps) => {
  return [
    {
      label: fileProps.extension === ".folder" ? "打开文件夹" : "打开文件",
      click: () => handleDoubleClick(fileProps),
    },
    {
      label: "下载文件",
      disabled: fileProps.extension === ".folder",
      click: () => handleDownload(fileProps),
    },
    {
      label: "详细信息",
      click: () => handleFileInfo(fileProps),
    },
  ];
};

const regularMenuItems = () => {
  return [
    {
      label: "上传文件",
      click: handleUpload,
    },
    {
      label: "新建文件夹",
      click: handleCreateFolder,
    },
    {
      label: "刷新",
      click: () => getFiles(currentPath.value),
    },
  ];
};

const handleContextMenuPrevent = (
  event: MouseEvent,
  fileProps: FileProps | null = null,
) => {
  menuIsOpen.value = false;
  if (fileProps !== null && fileProps !== undefined) {
    menuItems.value = fileMenuItems(fileProps);
  } else {
    menuItems.value = regularMenuItems();
  }
  menuEvent.value = event;
  setTimeout(() => {
    menuIsOpen.value = true;
  }, 10);
};
//endregion

onMounted(() => {
  isListView.value = localStorage.getItem("isListView") === "true";
  canTouch.value = isTouchDevice();
  getFiles("/");
  getUploadParams();
});
</script>

<template>
  <div
    class="root-container"
    @contextmenu.stop.prevent="handleContextMenuPrevent"
  >
    <vue3-menus
      :open="menuIsOpen"
      :event="menuEvent"
      :menus="menuItems"
      @update:open="menuIsOpen = $event"
    />
    <div class="path-breadcrumb">
      <el-button :disabled="currentPath === '/'" @click="getFiles(lastPath)">
        <el-icon size="20">
          <FluentArrowLeft24Regular />
        </el-icon>
      </el-button>
      <el-breadcrumb :separator-icon="ArrowRight">
        <el-breadcrumb-item
          v-for="(item, index) in breadcrumbs"
          :key="index"
          @click="navigateTo(item.path)"
          >{{ item.label }}</el-breadcrumb-item
        >
      </el-breadcrumb>
    </div>
    <div class="menu-bar">
      <!--Left-->
      <div class="menu-bar-left">
        <el-button @click="handleUpload">
          <div class="menu-bar-button">
            <el-icon size="20">
              <FluentdocumentAdd24Filled />
            </el-icon>
            上传文件
          </div></el-button
        >
        <el-button @click="handleCreateFolder">
          <div class="menu-bar-button">
            <el-icon size="20">
              <FluentfolderAdd24Filled />
            </el-icon>
            新建文件夹
          </div></el-button
        >
      </div>
      <!--Right-->
      <div class="menu-bar-right">
        <el-tooltip content="搜索文件" placement="top">
          <el-button @click="showSearchBar = !showSearchBar">
            <el-icon size="20">
              <FluentSearch24Regular />
            </el-icon>
          </el-button>
        </el-tooltip>
        <el-button-group>
          <el-tooltip content="列表" placement="top">
            <el-button @click="handleViewMethod(true)" :disabled="isListView">
              <el-icon size="20">
                <FluentAppsList24Regular />
              </el-icon>
            </el-button>
          </el-tooltip>
          <el-tooltip content="图标" placement="top">
            <el-button @click="handleViewMethod(false)" :disabled="!isListView">
              <el-icon size="20">
                <FluentGrid24Regular />
              </el-icon>
            </el-button>
          </el-tooltip>
        </el-button-group>
        <el-tooltip content="刷新" placement="top">
          <el-button @click="getFiles(currentPath)">
            <el-icon size="20">
              <FluentArrowSync24Regular />
            </el-icon>
          </el-button>
        </el-tooltip>
      </div>
    </div>
    <div v-if="showSearchBar" class="search-bar">
      <el-input
        v-model="searchText"
        placeholder="搜索文件..."
        :prefix-icon="Search"
        class="search-input"
        clearable
      />
    </div>
    <!--图标-->
    <div v-if="filteredFiles.length !== 0 && !isListView" class="file-grid">
      <FileComponent
        v-for="file in filteredFiles"
        :name="file.name"
        :file-id="file.fileId"
        :extension="file.extension"
        @click="handleClick"
        @double-click="handleDoubleClick"
        @open-contextmenu="handleContextMenuPrevent"
        @checked="handleFileComponentChecked"
      />
    </div>
    <!--表格-->
    <div v-else-if="filteredFiles.length !== 0 && isListView" class="file-list">
      <el-table
        :data="filteredFiles"
        :default-sort="{ prop: 'date', order: 'descending' }"
        highlight-current-row
        show-overflow-tooltip
        style="width: 100%"
        :row-style="{ height: '64px' }"
        @row-click="handleClick"
        @row-dblclick="handleDoubleClick"
        @row-contextmenu="
          (row: FileProps, column: any, event: MouseEvent) => {
            column;
            event.stopPropagation();
            event.preventDefault();
            handleContextMenuPrevent(event, row);
          }
        "
        @selection-change="
          (val: FileProps[]) => {
            multipleSelection = val;
            const file = val[val.length - 1];
            if (file) {
              currentSelection = file;
            }
          }
        "
      >
        <!--复选框-->
        <el-table-column
          :selectable="(row: FileProps) => row.extension !== '.folder'"
          type="selection"
          width="Auto"
        />

        <!--文件名-->
        <el-table-column prop="name" label="名称" sortable width="Auto">
          <template #default="scope">
            <div style="display: flex; align-items: center; gap: 8px">
              <FileIcon :extension="scope.row.extension" :size="24" />
              {{ scope.row.name }}
            </div>
          </template>
        </el-table-column>

        <!--类型-->
        <el-table-column prop="name" label="类型" width="100" align="center">
          <template #default="scope">
            <el-tag
              v-if="fileType(scope.row.extension) === 'folder'"
              type="warning"
            >
              文件夹
            </el-tag>
            <el-tag v-else type="success"> 文件 </el-tag>
          </template>
        </el-table-column>
      </el-table>
    </div>
    <el-empty
      v-else
      :description="searchText === '' ? '空文件夹' : '没有与搜索条件匹配的项'"
    />
    <el-upload
      drag
      multiple
      class="upload"
      ref="uploadRef"
      :auto-upload="true"
      :on-success="() => getFiles(currentPath)"
      :data="{ path: currentPath }"
      action="/api/transmit/upload"
    >
      <div class="el-upload__text">
        点击 <em>上传文件</em> or 拖拽 到此处上传文件
      </div>
      <div class="el-upload__tip">
        支持批量上传，最大支持 {{ formatFileSize(uploadMaxSize) }}
      </div>
    </el-upload>
    <FileInfoView
      :open="openFileInfoView"
      :name="fileInfo.name"
      :extension="fileInfo.extension"
      :size="fileInfo.size"
      :creationTime="fileInfo.creationTime"
      :lastModified="fileInfo.lastModified"
      @close="openFileInfoView = false"
    />
    <BottomMenuBar
      :visible="multipleSelection.length !== 0"
      :file-props="currentSelection"
      :open-button-disabled="multipleSelection.length !== 1"
      :download-button-disabled="false"
      :view-props-button-disabled="multipleSelection.length !== 1"
      @open-click="handleDoubleClick"
      @download-click="handleDownload"
      @view-props-click="handleFileInfo"
    />
    <el-image-viewer
      v-if="showImagePreview"
      :url-list="imageSrcList"
      show-progress
      :initial-index="imageIndex"
      @close="showImagePreview = false"
    />
  </div>
</template>

<style scoped>
@import "../components/context-menu.css";

.root-container {
  flex-grow: 1;
}

.path-breadcrumb {
  gap: 12px;
  display: flex;
  margin-left: 8px;
  margin-right: 8px;
  align-items: center;
}

.el-breadcrumb :deep(.el-breadcrumb__item:last-child .el-breadcrumb__inner) {
  color: #ffc53d;
  cursor: default;
  font-weight: bold;
}

.el-breadcrumb
  :deep(.el-breadcrumb__item:not(:last-child) .el-breadcrumb__inner:hover) {
  color: #ffc53d;
  cursor: pointer;
}

.menu-bar {
  display: flex;
  margin-top: 16px;
  margin-left: 8px;
  margin-right: 8px;
  justify-content: space-between;
}

.menu-bar-button {
  gap: 8px;
  display: flex;
  flex-direction: row;
  align-items: center;
}

.menu-bar-left {
  display: flex;
  align-items: center;
}

.menu-bar-right {
  display: flex;
  gap: 12px;
}

.search-bar {
  margin-top: 16px;
  margin-left: 8px;
  margin-right: 8px;
}

.file-grid {
  gap: 8px;
  display: grid;
  margin-top: 16px;
  align-items: center;
  justify-items: center;
  grid-template-columns: repeat(auto-fill, minmax(96px, 1fr));
}

.file-list {
  margin-top: 16px;
  margin-left: 8px;
  margin-right: 8px;
}

.upload {
  margin-top: 32px;
  margin-left: 8px;
  margin-right: 8px;
}

@media (max-width: 540px) {
  .menu-bar {
    gap: 16px;
    flex-direction: column;
  }
}
</style>
