<script setup lang="ts">
import axios from "axios";
import { Vue3Menus } from "vue3-menus";
import { onMounted, ref, shallowRef } from "vue";
import { formatFileSize } from "../helpers/CustomHelper.ts";
import {
  ElLoading,
  ElMessageBox,
  ElNotification,
  type UploadFile,
  type UploadFiles,
  type UploadInstance,
  type UploadRawFile,
} from "element-plus";

const uploadRef = ref<UploadInstance>();
const uploadMaxSize = ref(0);
const requestTimeout = ref(35); //管理员响应超时时间（s）
const requestResult = ref(0);
const requestUuid = ref("");
const guestName = ref("");
const uploadFiles = ref<UploadRawFile[]>([]);
const uploadUrl = ref("");
const fileCount = ref(0);

const showForbiddenMessage = () => {
  ElMessageBox.alert("未启用访客上传功能，请联系管理员开启。", "提示", {
    type: "warning",
    center: true,
    confirmButtonText: "确认",
  });
};

const getUploadParams = async () => {
  await axios
    .get(`/api/common/parameter`)
    .then((response) => {
      if (!response.headers["content-type"].includes("application/json")) {
        ElNotification.error({
          title: "错误",
          message: "获取上传参数失败。",
        });
        return;
      }
      uploadMaxSize.value = response.data.maxFileSize * 1024 * 1024;
      requestTimeout.value = (response.data.requestTimeout ?? 30) + 5;
    })
    .catch((error) => {
      if (error.response.status === 403) {
        showForbiddenMessage();
      } else {
        ElNotification.error({
          title: "错误",
          message: "获取上传参数失败。",
        });
      }
    });
};

//region 事件处理相关方法
const handleSelectFile = () => {
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

const handleRequestUpload = async () => {
  requestResult.value = 0;
  requestUuid.value = "";
  fileCount.value = uploadFiles.value.length;
  if (guestName.value === "") {
    await handleModifyGuestName();
    return;
  }
  const fileTotalSize = ref(0);
  uploadFiles.value.forEach((file) => {
    fileTotalSize.value += file.size;
  });
  const firstFile = ref<UploadRawFile>(uploadFiles.value[0]!);
  const formData = new FormData();
  formData.append("guestName", guestName.value);
  formData.append("firstFileName", firstFile.value.name);
  formData.append("fileCount", uploadFiles.value.length.toString());
  formData.append("fileTotalSize", formatFileSize(fileTotalSize.value));
  await axios
    .post(`/api/guest/request-upload`, formData)
    .then((response) => {
      if (!response.headers["content-type"].includes("application/json")) {
        ElNotification.error({
          title: "错误",
          message: "服务器返回的数据异常。",
        });
      }
      requestUuid.value = response.data.uuid;
      queryRequestResult();
    })
    .catch((error) => {
      if (error.response.status === 403) {
        showForbiddenMessage();
      } else {
        ElNotification.error({
          title: "错误",
          message: "请求上传失败。",
        });
      }
    });
};

const handleSendMessage = async () => {
  if (guestName.value === "") {
    await handleModifyGuestName();
    return;
  }

  try {
    const { value: message } = await ElMessageBox.prompt(
      "请输入需要发送给管理员的文本内容。",
      "发送文本",
      {
        confirmButtonText: "发送",
        cancelButtonText: "取消",
        inputType: "textarea",
        inputPlaceholder: "在此输入文本内容...",
        inputValidator: (value) => {
          if (!value || value.trim() === "") {
            return "文本内容不能为空";
          }
          if (value.length > 10000) {
            return "文本内容不能超过10000个字符";
          }
          return true;
        },
      },
    );

    const formData = new FormData();
    formData.append("guestName", guestName.value);
    formData.append("message", message.trim());

    await axios
      .post(`/api/guest/send-message`, formData)
      .then(() => {
        ElNotification.success({
          title: "成功",
          message: "文本已成功发送。",
        });
      })
      .catch((error) => {
        if (error.response.status === 403) {
          showForbiddenMessage();
        } else {
          ElNotification.error({
            title: "错误",
            message: "文本发送失败。",
          });
        }
      });
  } catch (error) {
    // 用户取消操作，不需要处理
  }
};

const handleModifyGuestName = async () => {
  ElMessageBox.prompt(
    "请在下方输入访客名称，以方便管理员区分发送者。",
    "设置访客名称",
    {
      confirmButtonText: "确认",
      cancelButtonText: "取消",
      inputPattern: /^[^\\\/:*?"<>|]*$/,
      inputErrorMessage: '访客名称不能包含以下字符：\\ / : * ? " < > |',
      inputPlaceholder: "访客名称",
      inputValidator: (value) => {
        if (!value || value.trim() === "") {
          return "访客名称不能为空";
        }
        if (value.length > 16) {
          return "访客名称不能超过16个字符";
        }
        return true;
      },
    },
  )
    .then(({ value }) => {
      // 去除首尾空格
      guestName.value = value.trim();
      localStorage.setItem("guestName", guestName.value);
    })
    .catch(() => {});
};
//endregion

//region 右键菜单相关变量、方法等。
const menuIsOpen = ref(false);
const menuEvent = ref<MouseEvent | undefined>({
  clientX: 0,
  clientY: 0,
} as MouseEvent);
const menuItems = shallowRef<any[]>([]);

const regularMenuItems = () => {
  return [
    {
      label: "选择上传文件",
      click: handleSelectFile,
    },
  ];
};

const handleContextMenuPrevent = (event: MouseEvent) => {
  menuIsOpen.value = false;
  menuItems.value = regularMenuItems();
  menuEvent.value = event;
  setTimeout(() => {
    menuIsOpen.value = true;
  }, 10);
};
//endregion

//region 定期查询请求结果
const queryRequestResult = () => {
  const totalTime = ref(0);
  const loading = ElLoading.service({
    lock: true,
    text: `已发送上传请求，请耐心等待管理员同意（${requestTimeout.value}s）...`,
    background: "rgba(0, 0, 0, 0.7)",
  });

  // 等待请求结果倒计时
  const waitRequestResultLoadingTimer = setInterval(() => {
    totalTime.value += 1;
    const remainingTime = requestTimeout.value - totalTime.value;
    if (remainingTime <= 0) {
      loading.close();
      clearInterval(queryRequestResultTimer);
      clearInterval(waitRequestResultLoadingTimer);
      ElMessageBox.alert("请求超时未响应，请联系管理员。", "提示", {
        type: "warning",
        center: true,
        confirmButtonText: "确认",
      });
    } else if (requestResult.value != 0) {
      loading.close();
      clearInterval(queryRequestResultTimer);
      clearInterval(waitRequestResultLoadingTimer);
      if (requestResult.value === 2) {
        ElMessageBox.alert("管理员拒绝了你的上传请求。", "提示", {
          type: "warning",
          center: true,
          confirmButtonText: "确认",
        });
      } else {
        uploadUrl.value = `/api/guest/upload/${requestUuid.value}`;
        uploadRef.value!.submit();
      }
    } else {
      loading.text.value = `已发送上传请求，请耐心等待管理员同意（${remainingTime}s）...`;
    }
  }, 1000);

  // 定期查询请求结果
  const queryRequestResultTimer = setInterval(() => {
    const formData = new FormData();
    formData.append("uuid", requestUuid.value);
    axios
      .post("api/guest/request-result", formData)
      .then((response) => {
        if (!response.headers["content-type"].includes("application/json")) {
          ElNotification.error({
            title: "错误",
            message: "服务器返回的数据异常。",
          });
        }
        requestResult.value = response.data.result;
      })
      .catch((error) => {
        if (error.response.status === 403) {
          showForbiddenMessage();
        }
      });
  }, 1000);
};
//endregion

const handleUploadChange = (file: UploadFile, fileList: UploadFile[]) => {
  if (file.size! > uploadMaxSize.value) {
    ElMessageBox.alert(
      `${file.name}文件体积超过了上传限制${formatFileSize(uploadMaxSize.value)}，请重新选择或者联系管理员调整上传限制。`,
      "提示",
      {
        type: "warning",
        center: true,
        confirmButtonText: "确认",
      },
    );
    uploadRef.value!.handleRemove(file);
    return;
  }

  uploadFiles.value = fileList
    .map((item) => item.raw as UploadRawFile)
    .filter(Boolean);
};

const handleUploadSuccess = () => {
  fileCount.value--;
  if (fileCount.value <= 0) {
    uploadRef.value!.clearFiles();
    ElNotification.success({ title: "成功", message: "所有文件上传成功。" });
  }
};

const handleUploadError = (
  _error: Error,
  uploadFile: UploadFile,
  _uploadFiles: UploadFiles,
) => {
  fileCount.value--;
  ElNotification.error({
    title: "失败",
    message: `${uploadFile.raw?.name}上传失败。`,
  });
  if (fileCount.value <= 0) {
    uploadRef.value!.clearFiles();
  }
};

onMounted(() => {
  getUploadParams();
  guestName.value = localStorage.getItem("guestName") ?? "";
  if (guestName.value == "") {
    handleModifyGuestName();
  }
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
    <div class="menu-bar">
      <!--Left-->
      <div class="menu-bar-left">
        <div>
          <el-button @click="handleSelectFile">
            <div class="menu-bar-button">
              <el-icon size="20">
                <FluentdocumentAdd24Filled />
              </el-icon>
              选择文件
            </div></el-button
          >
          <el-button
            :disabled="!uploadFiles.length"
            @click="handleRequestUpload"
          >
            <div class="menu-bar-button">
              <el-icon size="20">
                <FluentArrowUpload24Filled />
              </el-icon>
              开始上传
            </div></el-button
          >
        </div>
        <div class="menu-bar-left">
          <el-button @click="handleSendMessage">
            <div class="menu-bar-button">
              <el-icon size="20">
                <FluentCommentText24Filled />
              </el-icon>
              发送文本
            </div></el-button
          >
        </div>
      </div>
      <!--Right-->
      <div class="menu-bar-right">
        <div style="display: flex; flex-direction: row; gap: 12px">
          <el-text line-clamp="1">访客名：{{ guestName }}</el-text>
          <el-button @click="handleModifyGuestName"
            ><div class="menu-bar-button">
              <el-icon size="20">
                <FluentRename24Filled />
              </el-icon>
              重命名
            </div>
          </el-button>
        </div>
      </div>
    </div>
    <el-upload
      drag
      multiple
      class="upload"
      ref="uploadRef"
      :auto-upload="false"
      :action="uploadUrl"
      @change="handleUploadChange"
      :on-error="handleUploadError"
      :on-success="handleUploadSuccess"
    >
      <div class="el-upload__text">
        点击 <em>上传文件</em> or 拖拽 到此处上传文件
      </div>
      <div class="el-upload__tip">
        支持批量上传，单个文件最大支持 {{ formatFileSize(uploadMaxSize) }}
      </div>
    </el-upload>
  </div>
</template>

<style scoped>
@import "../components/context-menu.css";

.root-container {
  flex-grow: 1;
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
  gap: 12px;
}

.menu-bar-right {
  display: flex;
  gap: 12px;
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

  .menu-bar-left {
    flex-direction: column;
    align-items: start;
  }
}
</style>
