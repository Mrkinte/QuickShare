<script setup lang="ts">
import { useDark, useToggle } from "@vueuse/core";
import { Sunny, Moon } from "@element-plus/icons-vue";
import { onMounted } from "vue";
import axios from "axios";
import { v4 } from "uuid";
const isDark = useDark();
const toggleDark = useToggle(isDark);

const getOrCreateVisitorId = () => {
  let id = localStorage.getItem("visitorId");
  if (!id) {
    id = v4();
    localStorage.setItem("visitorId", id);
  }
  return id;
};

onMounted(async () => {
  const apiUrl = `/api/transmit/alive/${getOrCreateVisitorId()}`;
  await axios.get(apiUrl);

  setInterval(async () => {
    await axios.get(apiUrl);
  }, 5000);
});
</script>

<template>
  <div>
    <el-container class="root-container">
      <el-header class="root-container-header">
        <div class="title">
          <img
            class="title-icon"
            width="32"
            height="32"
            src="./assets/favicon.svg"
            alt="Logo"
          />
          <el-text class="title-text">QuickShare</el-text>
        </div>
        <div class="root-container-header-actions">
          <el-button @click="toggleDark()">
            <el-icon>
              <Sunny v-if="isDark" />
              <Moon v-else />
            </el-icon>
            <span>{{ isDark ? "亮色" : "暗色" }}</span>
          </el-button>
        </div>
      </el-header>
      <el-main class="root-container-main">
        <router-view />
      </el-main>
      <el-footer class="root-container-footer">
        <div class="github-link">
          <svg
            aria-hidden="true"
            focusable="false"
            viewBox="0 0 24 24"
            width="24"
            height="24"
            fill="currentColor"
            overflow="visible"
            style="vertical-align: text-bottom"
          >
            <path
              d="M12 1C5.923 1 1 5.923 1 12c0 4.867 3.149 8.979 7.521 10.436.55.096.756-.233.756-.522 0-.262-.013-1.128-.013-2.049-2.764.509-3.479-.674-3.699-1.292-.124-.317-.66-1.293-1.127-1.554-.385-.207-.936-.715-.014-.729.866-.014 1.485.797 1.691 1.128.99 1.663 2.571 1.196 3.204.907.096-.715.385-1.196.701-1.471-2.448-.275-5.005-1.224-5.005-5.432 0-1.196.426-2.186 1.128-2.956-.111-.275-.496-1.402.11-2.915 0 0 .921-.288 3.024 1.128a10.193 10.193 0 0 1 2.75-.371c.936 0 1.871.123 2.75.371 2.104-1.43 3.025-1.128 3.025-1.128.605 1.513.221 2.64.111 2.915.701.77 1.127 1.747 1.127 2.956 0 4.222-2.571 5.157-5.019 5.432.399.344.743 1.004.743 2.035 0 1.471-.014 2.654-.014 3.025 0 .289.206.632.756.522C19.851 20.979 23 16.854 23 12c0-6.077-4.922-11-11-11Z"
            ></path>
          </svg>
          <el-link href="https://github.com/mrkinte/QuickShare" target="_blank"
            >Github</el-link
          >
        </div>
      </el-footer>
    </el-container>
  </div>
</template>

<style scoped>
.root-container {
  display: flex;
  user-select: none;
  flex-direction: column;
  min-height: calc(100vh - 180px);
}

.root-container-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.title {
  display: flex;
  flex-direction: row;
  gap: 0.5rem;
}

.title-text {
  font-weight: bold;
  font-size: 1.2rem;
}

.root-container-main {
  flex: 1;
  overflow-y: auto;
}

.root-container-footer {
  display: flex;
  justify-content: center;
  align-items: center;
}

.github-link {
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 0.5rem;
}

@media (max-width: 540px) {
  .root-container-main {
    padding: 20px 5px;
  }
}
</style>
