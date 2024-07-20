<template>
    <el-card v-if="isVisible" class="layer-card">
        <template v-slot:header>
            <div class="clearfix">
                <span>下載圖資</span>
                <el-button @click="toggleVisibility" class="close-btn" type="default">關閉</el-button>
            </div>
        </template>
        <el-input v-model="inputValue" placeholder="輸入資料的名稱" style="margin-bottom: 20px;"></el-input>
        <el-row>
            <el-col :span="12">
                <el-button type="primary" @click="downloadSHP">下載為shp檔案</el-button>
            </el-col>
            <el-col :span="12">
                <el-button type="primary" @click="downloadKML">下載為kml檔案</el-button>
            </el-col>
        </el-row>
    </el-card>
</template>

<script lang="ts" setup>
import { computed, ref, watch } from 'vue'
import { useCardStore } from '@/stores/CardLayer'
import { ElMessage } from 'element-plus'
import axios from 'axios'

const CardLayer = useCardStore()
const isVisible = computed(() => CardLayer.DownloadAction)
const inputValue = ref('')

// 監聽 isVisible 的變化，當變為 false 時清空暫存檔案
watch(isVisible, (newValue) => {
    if (!newValue) {
        inputValue.value = '';
    }
})
const toggleVisibility = () => {
    CardLayer.DownloadCard()
}

const downloadFile = async (urlType: string, fileType: string) => {
    try {
        const response = await axios({
            method: 'post',
            url: urlType,
            data: inputValue.value,
            headers: { 'Content-Type': 'application/json' },
            responseType: 'blob'
        })
        const disposition = response.headers['content-disposition']
        if (disposition === undefined) {
            // 使用 Element Plus 的 ElMessage 提示無法下載檔案
            ElMessage.error(`該檔案不支持${fileType}檔案的下載`)
            return
        }
        const fileName = disposition.split(';')[1].split('=')[1].trim()
        const url = window.URL.createObjectURL(new Blob([response.data], { type: response.headers['content-type'] }))
        const link = document.createElement('a')
        link.href = url
        link.setAttribute('download', fileName.trim())
        document.body.appendChild(link)
        link.click()
        document.body.removeChild(link)
        ElMessage.success(`下載${fileType}檔案成功`)
    } catch (error) {
        console.error(error)
    }
}

const downloadSHP = () => {
    downloadFile('/api/ShpDownload/ExportSHP', 'shp')
}

const downloadKML = () => {
    downloadFile('/api/KmlDownload/ExportKml', 'kml')
}
</script>

<style scoped>
.layer-card {
    position: absolute;
    top: 120px;
    left: 220px;
    width: 400px;
    height: 200px;
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