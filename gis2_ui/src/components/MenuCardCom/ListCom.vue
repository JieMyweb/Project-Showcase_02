<template>
    <el-card v-if="isVisible" class="layer-card">
        <template v-slot:header>
            <div class="clearfix">
                <span>圖資</span>
                <el-button @click="toggleVisibility" class="close-btn" type="default">關閉</el-button>
            </div>
        </template>
        <ul>
            <li v-for="(item, index) in listData" :key="index">{{ item }}</li>
        </ul>
    </el-card>
</template>

<script lang="ts" setup>
import { computed, ref, onMounted, watch } from 'vue'
import { useCardStore } from '@/stores/CardLayer'

const CardLayer = useCardStore()
const isVisible = computed(() => CardLayer.ListAction)
const listData = ref([])

// 監聽 pinia 中的數值變化
watch(() => CardLayer.RenewCardAct, () => {
    // 在 pinia 中的數值變化時觸發刷新操作
    refreshListData()
})
const toggleVisibility = () => {
    CardLayer.ListCard()
}

const refreshListData = async () => {
    // Call API to fetch updated list data
    const response = await fetch('./api/FileList/GetTypeList')
    const data = await response.json()
    listData.value = data
}

onMounted(() => {
    // 初始化時獲取初始列表數據
    refreshListData()
})


</script>

<style scoped>
.layer-card {
    position: absolute;
    top: 120px;
    left: 220px;
    width: 400px;
    z-index: 1000;
    background-color: white;
    border: 2px solid #eeeeee;
    border-radius: 10px;
    box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
}

.close-btn {
    float: right;
    padding: 0;
    width: 40px;
    height: 40px;
    margin-top: -10px;
    border-radius: 7px;
    box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
}
</style>