<script setup lang="ts">
import axios from "axios";
import { Vue3Menus } from "vue3-menus";
import FileComponent from "../components/FileComponent.vue";
import { ArrowRight, Search } from "@element-plus/icons-vue";
import { computed, onMounted, ref, shallowRef } from "vue";
import {
  downloadFile,
  getApiBaseUrl,
  isTouchDevice,
  sleep,
} from "../helpers/CustomHelper.ts";
import { ElNotification } from "element-plus";
import { type FileProps, fileType } from "../helpers/FileHelper.ts";
import FileIcon from "../components/FileIcon.vue";
import FileInfoView from "../components/FileInfoView.vue";
import BottomMenuBar from "../components/BottomMenuBar.vue";
import router from "../router";

const canTouch = ref(false);
const isListView = ref(false);
const verificationCode = ref("");

const multipleSelection = ref<FileProps[]>([]);
const currentSelection = ref<FileProps>({ name: "", fileId: 0, extension: "" });

const imageSrcList = ref<string[]>([]);
const showImagePreview = ref(false);

const props = defineProps<{ shareId: string }>();
type shareProps = {
  description: string;
  createTime: string;
  fileCount: number;
  directoryCount: number;
};
const shareInfo = ref<shareProps>({
  description: "",
  createTime: "",
  fileCount: 0,
  directoryCount: 0,
});

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
const getShare = async () => {
  const formData = new FormData();
  formData.append("verificationCode", verificationCode.value);
  await axios
    .post(`/api/share/info/${props.shareId}`, formData)
    .then((response) => {
      if (!response.headers["content-type"].includes("application/json")) {
        ElNotification.error({
          title: "错误",
          message: "服务器返回的数据异常。",
        });
      }
      shareInfo.value = response.data;
      getFiles();
    })
    .catch((error) => {
      if (error.response.status === 401) {
        router.push(`/verify/${props.shareId}`);
      } else if (error.response.status === 404) {
        router.push(`/invalid-share/${props.shareId}`);
      }
    });
};

const getFiles = async (fileId: number = 0) => {
  searchText.value = "";
  const formData = new FormData();
  formData.append("fileId", fileId.toString());
  formData.append("verificationCode", verificationCode.value);
  await axios
    .post(`/api/share/files/${props.shareId}`, formData)
    .then((response) => {
      if (!response.headers["content-type"].includes("application/json")) {
        ElNotification.error({
          title: "错误",
          message: "服务器返回的数据异常。",
        });
      }
      files.value = response.data;
    })
    .catch((error) => {
      if (error.response.status === 401) {
        router.push(`/verify/${props.shareId}`);
      } else if (error.response.status === 404) {
        router.push(`/invalid-share/${props.shareId}`);
      } else if (error.response.status === 500) {
        ElNotification.error({ title: "错误", message: "服务器内部错误。" });
      }
    });
};

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

const getFileInfo = async (fileId: number = 0) => {
  const formData = new FormData();
  formData.append("fileId", fileId.toString());
  formData.append("verificationCode", verificationCode.value);
  await axios
    .post(`/api/share/file-info/${props.shareId}`, formData)
    .then((response) => {
      if (!response.headers["content-type"].includes("application/json")) {
        ElNotification.error({
          title: "错误",
          message: "服务器返回的数据异常。",
        });
      }
      fileInfo.value = response.data;
      openFileInfoView.value = true;
    })
    .catch((error) => {
      if (error.response.status === 401) {
        router.push(`/verify/${props.shareId}`);
      } else if (error.response.status === 404) {
        router.push(`/invalid-share/${props.shareId}`);
      } else if (error.response.status === 500) {
        ElNotification.error({ title: "错误", message: "服务器内部错误。" });
      }
    });
};

const getDownloadTicket = async (
  fileId: number = 0,
): Promise<string | null> => {
  try {
    const formData = new FormData();
    formData.append("fileId", fileId.toString());
    formData.append("verificationCode", verificationCode.value);

    const response = await axios.post(
      `/api/share/ticket/${props.shareId}`,
      formData,
    );

    if (!response.headers["content-type"].includes("application/json")) {
      ElNotification.error({
        title: "错误",
        message: "服务器返回的数据异常。",
      });
      return null;
    }

    return response.data.ticket;
  } catch (error: any) {
    if (error.response?.status === 401) {
      await router.push(`/verify/${props.shareId}`);
    } else if (error.response?.status === 500) {
      ElNotification.error({
        title: "错误",
        message: "服务器内部错误。",
      });
    }
    return null;
  }
};
//endregion

//region 事件处理相关方法
const handleClick = (fileProps: FileProps) => {
  if (canTouch.value) {
    handleDoubleClick(fileProps);
  }
};

const handleDoubleClick = async (fileProps: FileProps) => {
  if (fileProps.extension === ".folder") {
    addBreadcrumbItem(fileProps);
    await getFiles(fileProps.fileId);
  } else if (fileType(fileProps.extension) === "image") {
    imageSrcList.value = [];
    const ticket = await getDownloadTicket(fileProps.fileId);
    files.value.forEach((file) => {
      if (fileProps.name === file.name) {
        const fullSrc =
          getApiBaseUrl() +
          `/api/share/download/${props.shareId}/${fileProps.fileId}/${ticket}`;
        imageSrcList.value.push(fullSrc);
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
  if (multipleSelection.value.length <= 1) {
    const ticket = await getDownloadTicket(fileProps.fileId);
    downloadFile(
      `/api/share/download/${props.shareId}/${fileProps.fileId}/${ticket}`,
    );
  } else {
    for (const file of multipleSelection.value) {
      const ticket = await getDownloadTicket(file.fileId);
      downloadFile(
        `/api/share/download/${props.shareId}/${file.fileId}/${ticket}`,
      );
      await sleep(500); // 延时 500ms
    }
  }
};

const handleFileInfo = (fileProps: FileProps) => {
  getFileInfo(fileProps.fileId);
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
interface BreadcrumbItem {
  label: string;
  index: number;
  fileId: number;
}

const breadcrumbs = ref<BreadcrumbItem[]>([
  { label: "文件", index: 0, fileId: 0 },
]);

const navigateTo = (item: BreadcrumbItem) => {
  getFiles(item.fileId);
  breadcrumbs.value = breadcrumbs.value.filter((i) => i.index <= item.index);
};

const addBreadcrumbItem = (fileProps: FileProps) => {
  if (breadcrumbs.value.some((item) => item.fileId === fileProps.fileId)) {
    return;
  }
  breadcrumbs.value.push({
    label: fileProps.name,
    index: breadcrumbs.value.length,
    fileId: fileProps.fileId,
  });
};

const backLastDirectory = () => {
  const item = breadcrumbs.value[breadcrumbs.value.length - 2];
  if (item === null || item === undefined) {
    return;
  }
  getFiles(item.fileId);
  breadcrumbs.value = breadcrumbs.value.filter((i) => i.index <= item.index);
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
      label: "属性",
      click: () => handleFileInfo(fileProps),
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
    menuEvent.value = event;
    setTimeout(() => {
      menuIsOpen.value = true;
    }, 10);
  }
};
//endregion

onMounted(() => {
  verificationCode.value = sessionStorage.getItem(props.shareId) ?? "";
  isListView.value = localStorage.getItem("isListView") === "true";
  canTouch.value = isTouchDevice();
  getShare();
});
</script>

<template>
  <div class="root-container">
    <vue3-menus
      :open="menuIsOpen"
      :event="menuEvent"
      :menus="menuItems"
      @update:open="menuIsOpen = $event"
    />
    <div class="share-info">
      <el-descriptions border title="分享信息" :column="2">
        <el-descriptions-item label="创建时间">{{
          shareInfo.createTime
        }}</el-descriptions-item>
        <el-descriptions-item label="包含"
          >{{ shareInfo.fileCount }}个文件，{{
            shareInfo.directoryCount
          }}个文件夹</el-descriptions-item
        >
        <el-descriptions-item label="备注">{{
          shareInfo.description
        }}</el-descriptions-item>
      </el-descriptions>
    </div>
    <div class="path-breadcrumb">
      <el-button
        :disabled="breadcrumbs.length === 1"
        @click="backLastDirectory"
      >
        <el-icon size="20">
          <FluentArrowLeft24Regular />
        </el-icon>
      </el-button>
      <el-breadcrumb :separator-icon="ArrowRight">
        <el-breadcrumb-item
          v-for="(item, index) in breadcrumbs"
          :key="index"
          @click="navigateTo(item)"
          >{{ item.label }}</el-breadcrumb-item
        >
      </el-breadcrumb>
    </div>
    <div class="menu-bar">
      <!--Left-->
      <div class="menu-bar-left">
        <el-text v-if="multipleSelection.length > 0"
          >共选择 {{ multipleSelection.length }} 个文件</el-text
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
          <el-button
            @click="getFiles(breadcrumbs[breadcrumbs.length - 1]?.fileId)"
          >
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
      :initial-index="0"
      @close="showImagePreview = false"
    />
  </div>
</template>

<style scoped>
@import "../components/context-menu.css";

.root-container {
  flex-grow: 1;
}

.share-info {
  margin-left: 8px;
  margin-right: 8px;
}

.path-breadcrumb {
  gap: 12px;
  display: flex;
  margin-top: 16px;
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

@media (max-width: 540px) {
  .menu-bar {
    gap: 16px;
    flex-direction: column;
  }
}
</style>
