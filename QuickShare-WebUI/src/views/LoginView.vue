<script setup lang="ts">
import { onMounted, ref } from "vue";
import axios from "axios";
import { useRouter } from "vue-router";
import { ElMessageBox, ElNotification } from "element-plus";

const router = useRouter();
const logging = ref(false);
const password = ref("");
const enableGuest = ref(false);

const getUploadParams = async () => {
  await axios.get(`/api/common/parameter`).then((response) => {
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
    enableGuest.value = response.data.enableGuest;
  });
};

const handleLogin = async () => {
  if (password.value === "") {
    ElNotification({
      title: "登录失败",
      message: "密码不能为空，请重新输入。",
      type: "warning",
    });
    return;
  }

  const formData = new FormData();
  formData.append("password", password.value);
  logging.value = true;
  await axios
    .post("/api/transmit/login", formData)
    .then((response) => {
      if (response.data.message === "Successful") {
        router.push("/transmit");
      }
    })
    .catch((error) => {
      if (error.response.status === 401) {
        ElNotification({
          title: "登录失败",
          message: "密码错误，请重新输入。",
          type: "error",
        });
      }
    })
    .finally(() => {
      logging.value = false;
    });
};

const handleGuestLogin = () => {
  router.push("/guest");
};

const handleForgetPassword = () => {
  ElMessageBox.alert(
    "如果忘记了登录密码，请在QuickShare客户端 -> 设置中修改。",
    "忘记密码？",
    {
      confirmButtonText: "确认",
    },
  );
};

onMounted(() => {
  getUploadParams();
});
</script>

<template>
  <div class="root-container">
    <el-card>
      <div class="login-card">
        <div class="title">
          <img
            class="title-icon"
            width="64"
            height="64"
            src="../assets/favicon.svg"
            alt="Logo"
          />
          <el-text class="title-text" style="color: cadetblue"
            >QuickShare</el-text
          >
          <el-text>欢迎使用QuickShare局域网文件传输助手</el-text>
        </div>
        <div class="login-form">
          <el-input
            v-model="password"
            class="password-input"
            placeholder="请输入您的密码"
            @keyup.enter="handleLogin"
            type="password"
            show-password
          />
          <el-button
            class="login-button"
            color="cadetblue"
            :loading="logging"
            @click="handleLogin"
            >登录系统</el-button
          >
          <el-button
            v-if="enableGuest"
            class="login-button"
            color="darkorange"
            @click="handleGuestLogin"
            >访客上传</el-button
          >
          <el-button
            style="align-self: center"
            link
            @click="handleForgetPassword"
            >忘记密码？</el-button
          >
        </div>
      </div>
    </el-card>
  </div>
</template>

<style scoped>
.root-container {
  display: flex;
  align-items: center;
  justify-content: center;
}

:deep(.el-card) {
  max-width: 680px;
  width: 100%;
  border-radius: 10px;
}

.login-card {
  gap: 4rem;
  height: 25vh;
  display: flex;
  padding: 10px 5px;
  flex-direction: row;
  align-items: center;
}

.login-form {
  gap: 1.4rem;
  display: flex;
  flex-grow: 1;
  flex-direction: column;
}

.title {
  gap: 0.5rem;
  display: flex;
  align-items: center;
  flex-direction: column;
}

.title-text {
  font-weight: bold;
  font-size: 1.6rem;
}

:deep(.el-input__wrapper) {
  border-radius: 10px;
}

:deep(.el-input) {
  height: 48px;
  font-size: 1rem;
}

.login-button {
  height: 48px;
  margin-left: 0;
  font-size: 1rem;
  border-radius: 10px;
}

@media (max-width: 540px) {
  :deep(.el-card) {
    width: 100%;
  }

  .login-card {
    gap: 2rem;
    height: 100%;
    padding: 10px 5px;
    flex-direction: column;
  }

  .login-form {
    width: 100%;
  }
}
</style>
