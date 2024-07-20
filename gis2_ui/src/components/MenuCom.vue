<template>
    <el-switch v-model="isCollapse" class="el-swich-but" />
    <el-menu class="el-menu-vertical" :collapse="!isCollapse" @open="handleOpen" @close="handleClose">
        <el-menu-item @click="toggleLayerCard">
            <el-icon>
                <MapLocation />
            </el-icon>
            <template #title>圖資</template>
        </el-menu-item>
        <el-menu-item @click="SearchSpot">
            <el-icon>
                <Search />
            </el-icon>
            <template #title>查詢地點</template>
        </el-menu-item>
        <el-menu-item @click="AddSpot">
            <el-icon>
                <Plus />
            </el-icon>
            <template #title>新增圖資</template>
        </el-menu-item>
        <el-menu-item @click="UpFile">
            <el-icon>
                <Paperclip />
            </el-icon>
            <template #title>上傳圖資</template>
        </el-menu-item>
        <el-menu-item @click="DownloadCard">
            <el-icon>
                <Download />
            </el-icon>
            <template #title>下載圖資</template>
        </el-menu-item>
    </el-menu>

    <ListCom />
    <DownloadCardCom />
    <UpFileCom />
</template>

<script lang="ts" setup>
import { ref } from 'vue'
import { Plus, Search, Paperclip, Download, MapLocation } from '@element-plus/icons-vue'
import { useSearchStore } from '@/stores'
import { useCardStore } from '@/stores/CardLayer'
import ListCom from './MenuCardCom/ListCom.vue'
import UpFileCom from './MenuCardCom/UpFileCom.vue'
import DownloadCardCom from './MenuCardCom/DownloadCardCom.vue'



const mainStore = useSearchStore()
const isCollapse = ref(true)
const handleOpen = (key: string, keyPath: string[]) => {
    console.log(key, keyPath)
}
const handleClose = (key: string, keyPath: string[]) => {
    console.log(key, keyPath)
}
//查詢功能透過pinia的布林值在MainCom方法間看決定是否執行查詢功能
const SearchSpot = () => {
    mainStore.SearchSpot()
}
//新增功能透過pinia的布林值在MainCom方法間看決定是否執行查詢功能
const AddSpot = () => {
    mainStore.AddSpot()
}


//pinia控制菜單欄卡片
const CardStore = useCardStore()
//圖層彈出卡片
const toggleLayerCard = () => {
    CardStore.ListCard()
}
//以開圖層彈出卡片
const DownloadCard = () => {
    CardStore.DownloadCard()
}
//上傳塗層彈出卡片
const UpFile = () => {
    CardStore.UpFileCard()
}
</script>

<style>
.el-menu-vertical:not(.el-menu--collapse) {
    width: 200px;
    min-height: 400px;
}

.el-swich-but {
    --el-switch-on-color: #13ce66;
    --el-switch-off-color: #969696;
    margin-left: -150px;
}
</style>