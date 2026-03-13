<script setup lang="ts">
import { ref } from "vue";
import axios from "axios";
import { useRouter } from "vue-router";
import { ElNotification } from "element-plus";

const props = defineProps<{ shareId: string }>();
const router = useRouter();
const logging = ref(false);
const verificationCode = ref("");

const handleLogin = async () => {
  const formData = new FormData();
  formData.append("verificationCode", verificationCode.value);
  logging.value = true;
  const url = "/api/share/verify/" + props.shareId;
  await axios
    .post(url, formData)
    .then((response) => {
      if (response.data.message === "Successful") {
        sessionStorage.setItem(props.shareId, verificationCode.value);
        router.push(`/share/${props.shareId}`);
      }
    })
    .catch((error) => {
      if (error.response.status === 401) {
        ElNotification({
          title: "验证失败",
          message: "验证码不正确，请重新输入。",
          type: "error",
        });
      }
    })
    .finally(() => {
      logging.value = false;
    });
};
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
            v-model="verificationCode"
            class="password-input"
            placeholder="请输入分享验证码"
            @keyup.enter="handleLogin"
            type="password"
            show-password
          />
          <el-button
            class="login-button"
            color="cadetblue"
            :loading="logging"
            @click="handleLogin"
            >查看分享</el-button
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
}
</style>
