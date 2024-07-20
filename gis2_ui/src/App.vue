<template>
  <HeaderCom />
  <router-view></router-view>
</template>

<script lang="ts">
import { defineComponent } from 'vue';
import HeaderCom from './components/HeaderCom.vue';
export default defineComponent({
  name: 'App',
  components: {
    HeaderCom,
  },
});
</script>

<style>
#app {
  font-family: Avenir, Helvetica, Arial, sans-serif;
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  text-align: center;
  color: #2c3e50;
}
</style>
