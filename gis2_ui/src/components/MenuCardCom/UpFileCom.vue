<template>
    <el-card v-if="isVisible" class="layer-upcard">
        <template v-slot:header>
            <div class="clearfix">
                <span>上傳圖資</span>
                <el-button @click="toggleVisibility" class="close-btn" type="default">關閉</el-button>
            </div>
        </template>
        <div class="card-content">
            <el-input v-model="inputValue" placeholder="定義上傳資料的名稱" style="margin-bottom: 20px;"></el-input>
            <el-upload class="upload-demo" drag :http-request="handleCustomRequest" multiple ref="uploadReset" :on-remove="handleRemove">
                <el-icon class="el-icon--upload"><upload-filled /></el-icon>
                <div class="el-upload__text">
                    Drop file here or <em>click to upload</em>
                </div>
                <template #tip>
                    <div class="el-upload__tip">
                        請上傳 .shp / .dbf / .shx / .prj /.kml 檔案格式，且最多上傳五個文件
                    </div>
                </template>
            </el-upload>
            <el-button @click="confirmUpload" type="primary" class="confirm-btn" :loading="loading">確定</el-button>
        </div>
    </el-card>
</template>

<script lang="ts" setup>
import { computed, ref, watch } from 'vue'
import { useCardStore } from '@/stores/CardLayer'
import { UploadFilled } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import type { ElUpload } from 'element-plus';
import axios from 'axios';

const CardLayer = useCardStore()
const isVisible = computed(() => CardLayer.UpFileAction)
const tempFiles = ref<File[]>([])
const loading = ref(false)
const inputValue = ref('')
const uploadReset = ref<InstanceType<typeof ElUpload> | null>(null)

const toggleVisibility = () => {
    CardLayer.UpFileCard()
}

// 處理自定義上傳請求，將檔案暫存在前端
const handleCustomRequest = (options: any) => {
    tempFiles.value.push(options.file)
    options.onSuccess({}, options.file)
}

// 處理刪除文件的事件，從暫存區中移除相應的文件
const handleRemove = (file: File) => {
    const index = tempFiles.value.findIndex(tempFile => tempFile.name === file.name && tempFile.size === file.size);
    if (index !== -1) {
        tempFiles.value.splice(index, 1);
    }
}

// 監聽 isVisible 的變化，當變為 false 時清空暫存檔案
watch(isVisible, (newValue) => {
    if (!newValue) {
        tempFiles.value = []
        inputValue.value = '';
    }
})

// 確認上傳，將暫存的檔案發送到後端 API
const confirmUpload = async () => {
    loading.value = true;
    const formData = new FormData();
    tempFiles.value.forEach((file) => {
        formData.append('上傳需要匯入的Shp與Kml', file);
    });
    formData.append('DataType', inputValue.value);

    try {
        const response = await axios.post('./api/UploadShpOrKml/UploadFiles', formData, {
        });
        console.log('Upload to server success:', response.data);
        // 清空暫存檔案
        tempFiles.value = [];
        // 清空使用者輸入的資料名稱
        inputValue.value = '';
        // 清空上傳文件預覽畫面
        uploadReset.value?.clearFiles();
        ElMessage({
            message: '上傳成功',
            type: 'success',
        })
        CardLayer.RenewListCard()
    } catch (error: unknown) {
        console.error('Upload to server failed:', error);
        let errorMessage = '上傳失敗';

        if (axios.isAxiosError(error) && error.response) {
            // 這裡檢查 error 是否為 AxiosError 並且具有 response 屬性
            if (error.response.data) {
                errorMessage = error.response.data;
            }
        }

        ElMessage({
            message: errorMessage,
            type: 'warning',
        });
    } finally {
        loading.value = false; // 關閉 Loading 狀態
    }
};

</script>

<style scoped>
.layer-upcard {
    position: absolute;
    top: 120px;
    left: 220px;
    width: 400px;
    height: 550px;
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

.card-content {
    max-height: 450px;
    /* 設置卡片內容的最大高度 */
    overflow-y: auto;
    /* 啟用垂直滾動條 */
}
</style>