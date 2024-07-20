<template>
    <div class="common-layout">
      <el-container>
        <el-aside width="200px"><MenuCom /></el-aside>
        <el-main><MainCom /></el-main>
      </el-container>
    </div>
  </template>

<script lang="ts">
import { defineComponent } from 'vue';
import MainCom from '../components/MainCom.vue';
import MenuCom from '../components/MenuCom.vue';
export default defineComponent({
  name: 'App',
  components: {
    MainCom,
    MenuCom,
  },
});
</script>