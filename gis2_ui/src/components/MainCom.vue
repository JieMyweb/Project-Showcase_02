<template>
    <div ref="mapContainer" class="map-container"></div>
    <div ref="mousePut" class="mouse-position"></div>
</template>

<script setup>
import { onMounted, ref, watch } from 'vue';
import 'ol/ol.css';
import { Map, View } from 'ol';
import TileLayer from 'ol/layer/Tile';
import OSM from 'ol/source/OSM';
import { fromLonLat } from 'ol/proj';
import VectorSource from 'ol/source/Vector';
import VectorLayer from 'ol/layer/Vector';
import Feature from 'ol/Feature';
import { Point } from 'ol/geom';
import ScaleLine from 'ol/control/ScaleLine';
import MousePosition from 'ol/control/MousePosition';
import { createStringXY } from 'ol/coordinate';
import { Cluster } from 'ol/source';
import { Circle as CircleStyle, Fill, Stroke, Text, Style, Icon } from 'ol/style';
import Select from 'ol/interaction/Select';
import { click } from 'ol/events/condition';
import { extend } from 'ol/extent';
import Swal from 'sweetalert2';
import { useSearchStore } from '@/stores'

import axios from 'axios';



const mapContainer = ref(null);
const mousePut = ref(null);
const map = ref(null);
const mainStore = useSearchStore()


//獲取地圖數據
const getSpotApi = () => {
    axios.get('./api/Spot/List')
        .then(res => {
            initMap(res.data);
        })
        .catch(err => {
            console.error('API error:', err);
        });
};

//初始化地圖
const initMap = (data) => {
    const vectorLayer = createVectorLayer(data);
    map.value = new Map({
        target: mapContainer.value,
        layers: [
            new TileLayer({
                source: new OSM(),
            }),
            vectorLayer,
        ],
        view: new View({
            center: fromLonLat([120.934390, 23.522040]),
            zoom: 7,
        }),
    });
    initControls();
    initSelect(vectorLayer);
};

const createVectorLayer = (data) => {
    //將數據中的每個點轉換成Feature物件，並設置其geometry為地理座標點
    const features = data
        .map(x => {
            const coordinates = x.geom.coordinates;
            const feature = new Feature({
                geometry: new Point(fromLonLat([coordinates[0], coordinates[1]])),
                name: x.name,
            });
            feature.setProperties(x);
            // console.log(feature.values_);
            return feature;
        });
    //使用這些Feature物件創建一個VectorSource
    const vectorSource = new VectorSource({ features });

    //創建一個Cluster資料源，並設置聚合的距離
    const clusterSource = new Cluster({
        distance: 30,
        source: vectorSource,
    });
    // console.log(clusterSource)
    //創建一個快取用於儲存不同大小的叢集樣式，避免每次重新計算
    const styleCache = {};

    //使用Cluster資料源創建一個向量圖層
    //樣式（style）設定為根據聚合內標記的數量來動態生成樣式。CircleStyle定義了聚合標記點的外觀
    //Text顯示了聚合中點的數量
    const vectorLayer = new VectorLayer({
        source: clusterSource,
        style: (feature) => {
            const size = feature.get('features').length;
            let style = styleCache[size];
            if (!style) {
                if (size === 1) {
                    style = new Style({
                        image: new Icon({
                            src: '/地圖圖標2.png', // 替換為您的自訂圖片的路徑
                            scale: 0.19,
                        }),
                    });
                } else {
                    style = new Style({
                        image: new CircleStyle({
                            radius: 15,
                            stroke: new Stroke({
                                color: '#fff',
                            }),
                            fill: new Fill({
                                color: '#3399CC',
                            }),
                        }),
                        text: new Text({
                            text: size.toString(),
                            fill: new Fill({
                                color: '#fff',
                            }),
                        }),
                    });
                }
                styleCache[size] = style;
            }
            return style;
        },
    });
    return vectorLayer;
};

//比例尺與滑鼠座標
const initControls = () => {
    const mouseControl = new MousePosition({
        coordinateFormat: createStringXY(4),
        projection: 'EPSG:4326',
        className: 'custom-mouse-position',
        target: mousePut.value,
        undefinedHTML: '&nbsp;',
    });

    map.value.addControl(new ScaleLine());
    map.value.addControl(mouseControl);
};

//處理標記點的選擇和縮放
const initSelect = (vectorLayer) => {
    // 創建一個Select交互，用於處理聚合點的點擊
    const selectClick = new Select({
        condition: click,
        layers: [vectorLayer]
    });

    selectClick.on('select', (event) => {
        if (event.selected.length > 0) {
            const feature = event.selected[0];
            const features = feature.get('features');

            if (features.length > 1) {
                // 如果選中的特徵是一個叢集，則計算其邊界並聚焦
                const extent = features[0].getGeometry().getExtent().slice();
                for (let i = 1; i < features.length; i++) {
                    extend(extent, features[i].getGeometry().getExtent());
                }
                map.value.getView().fit(extent, { duration: 1000 });
            } else {
                // 獲取選中標記的座標和名稱
                const selectedFeature = features[0];
                const name = selectedFeature.get('name');
                const geom = selectedFeature.get('geom');
                const px = geom.coordinates[0];
                const py = geom.coordinates[1];

                // 使用 SweetAlert2 彈出訊息框
                Swal.fire({
                    title: '景點資訊',
                    html: `
                    <strong>名稱：</strong> ${name}<br>
                    <strong>座標：</strong> ${px},${py}<br><br>
                    <button id="editButton" class="swal2-confirm swal2-styled" style="background-color: #3085d6;">編輯</button>
                    <button id="deleteButton" class="swal2-cancel swal2-styled" style="background-color: #d33;">刪除</button>
                    `,
                    //隱藏預設確定、取消按鈕
                    showConfirmButton: false,
                    showCancelButton: false,
                    icon: 'info'
                });

                // 按鈕事件監聽器
                document.getElementById('editButton').addEventListener('click', () => {
                    editFeature(selectedFeature);
                });

                document.getElementById('deleteButton').addEventListener('click', () => {
                    deleteFeature(selectedFeature);
                });

            }
        }
        selectClick.getFeatures().clear();
    });

    map.value.addInteraction(selectClick);
};

// 編輯標記點
const editFeature = (feature) => {
    const spotValue = feature.values_;
    const geom = spotValue.geom;
    const px = geom.coordinates[0];
    const py = geom.coordinates[1];
    Swal.fire({
        title: '編輯景點資訊',
        html: `
              <b>名稱：</b><input type="text" id="spot-name" class="swal2-input" value="${spotValue.name}">
              <b></br>X 座標：</b><input type="text" id="spot-x" class="swal2-input" value="${px}">
              <b></br>Y 座標：</b><input type="text" id="spot-y" class="swal2-input" value="${py}">`,
        showCancelButton: true,
        confirmButtonText: '確定',
        cancelButtonText: '取消',
        preConfirm: () => {
            const newName = document.getElementById('spot-name').value;
            const newX = document.getElementById('spot-x').value;
            const newY = document.getElementById('spot-y').value;
            return { newName, newX, newY };
        }
    }).then((result) => {
        if (result.isConfirmed) {
            axios({
                method: 'post',
                url: './api/Spot/editByid',
                data: { id: spotValue.id, name: result.value.newName, px: result.value.newX, py: result.value.newY }
            }).then(() => {
                console.log('更新成功');
                Swal.fire({
                    title: '編輯成功',
                    imageUrl: '../貓咪比讚.jpg',
                    imageWidth: 300,
                    imageHeight: 300,
                    confirmButtonText: '確定'
                });

                spotValue.name = result.value.newName;
                geom.coordinates[0] = result.value.newX;
                geom.coordinates[1] = result.value.newY;

                // 更新標記點的座標
                const newCoordinates = fromLonLat([parseFloat(result.value.newX), parseFloat(result.value.newY)]);
                feature.setGeometry(new Point(newCoordinates));

            }).catch((error) => {
                console.log('更新失敗', error);
            });
        }
    });
};

// 刪除標記點
const deleteFeature = (feature) => {
    const spotValue = feature.values_;
    axios.post(`./api/Spot/DeleteByid/${spotValue.id}`)
        .then(({ data }) => {
            console.log(data);
            Swal.fire({
                title: '刪除成功',
                imageUrl: '../老人比讚.jpg',
                confirmButtonText: '確定'
            }).then(() => {
                // 從 vectorSource 中移除該標記點
                const vectorLayer = map.value.getLayers().getArray().find(layer => layer instanceof VectorLayer);
                const vectorSource = vectorLayer.getSource().getSource(); // 獲取 clusterSource 的原始 source
                vectorSource.removeFeature(feature);
            });

        })
        .catch(error => {
            console.error('刪除失敗', error);
        });
};

//監聽stores.intdex.ts的布林值
//查詢、新增
watch(
    [() => mainStore.searchAction, () => mainStore.addAction],
    ([newSearchValue, newAddvalue]) => {
        if (newSearchValue) {
            searchSpot();
            mainStore.resetSearchSpot();
        }
        if (newAddvalue) {
            AddSpot();
            mainStore.resetAddSpot();
        }
    }
)

// 定義查詢功能
const searchSpot = () => {
    Swal.fire({
        title: '搜尋特定景點',
        input: 'text',
        inputPlaceholder: '輸入景點名稱或關鍵字',
        showCancelButton: true,
        cancelButtonText: '取消',
        confirmButtonText: '下一步'
    }).then((result) => {
        if (result.isConfirmed) {
            axios({
                method: 'post',
                url: './api/Spot/SearchSpots',
                data: { Name: result.value }
            }).then(res => {
                if (res.data && res.data.length > 0) {
                    const spots = res.data;
                    const spotListHtml = spots.map(spot => `<div class="spot-item" data-id="${spot.id}">${spot.name}</div>`).join('');
                    Swal.fire({
                        title: '搜尋結果',
                        html: `<div class="spot-list">${spotListHtml}</div>`,
                        showCancelButton: false,
                        showConfirmButton: false,
                        didOpen: () => {
                            document.querySelectorAll('.spot-item').forEach(item => {
                                item.style.cursor = 'pointer'; // 改變滑鼠指標
                                item.addEventListener('mouseenter', () => {
                                    item.style.backgroundColor = '#f0f0f0'; // 滑鼠移動到項目上方改變背景顏色
                                    item.style.color = 'blue'; // 改變字體顏色
                                });
                                item.addEventListener('mouseleave', () => {
                                    item.style.backgroundColor = ''; // 滑鼠移開後恢復背景顏色
                                    item.style.color = ''; // 恢復字體顏色
                                });
                                item.addEventListener('click', () => {
                                    const spotId = item.getAttribute('data-id');
                                    const selectedSpot = spots.find(spot => spot.id === spotId);
                                    if (selectedSpot) {
                                        const coordinates = fromLonLat([selectedSpot.geom.coordinates[0], selectedSpot.geom.coordinates[1]]);
                                        map.value.getView().animate({
                                            center: coordinates,
                                            zoom: 17,
                                            duration: 1000
                                        });
                                        Swal.close();
                                    }
                                });
                            });
                        }
                    });
                }
            }).catch(err => {
                console.log(err);
                Swal.fire({
                    title: '找不到該筆資料',
                    text: '請重新搜尋',
                    icon: 'warning',
                    confirmButtonText: '確定'
                });
            });
        }
    });
};

const AddSpot = () => {
    Swal.fire({
        title: '新增景點資訊',
        html: `
          <b>名稱：</b><input type="text" id="spot-name" class="swal2-input">
          <b></br>電話：</b><input type="text" id="spot-tel" class="swal2-input">
          <b></br>地址：</b><input type="text" id="spot-address" class="swal2-input">
          <b></br>X 座標：</b><input type="text" id="spot-x" class="swal2-input">
          <b></br>Y 座標：</b><input type="text" id="spot-y" class="swal2-input">`,
        showCancelButton: true,
        confirmButtonText: '確定',
        cancelButtonText: '取消',
        preConfirm: () => {
            const name = document.getElementById('spot-name').value;
            const tel = document.getElementById('spot-tel').value;
            const address = document.getElementById('spot-address').value;
            const px = parseFloat(document.getElementById('spot-x').value);
            const py = parseFloat(document.getElementById('spot-y').value);

            if (!name || !tel || !address || isNaN(px) || isNaN(py)) {
                Swal.showValidationMessage('請確保所有欄位均已填寫且座標資料為數字格式');
                return;
            }

            return { name, tel, address, px, py };
        }
    }).then((result) => {
        if (result.isConfirmed) {
            // 生成臨時ID
            //...為展開運算符
            const tempId = 'temp-' + Date.now();
            const newSpot = { ...result.value, id: tempId };

            // 創建新的 Feature
            const newFeature = new Feature({
                geometry: new Point(fromLonLat([newSpot.px, newSpot.py])),
                name: newSpot.name,
                tel: newSpot.tel,
                address: newSpot.address,
                geom: {
                    coordinates: [newSpot.px, newSpot.py]
                },
                id: newSpot.id
            });

            // 獲取 vectorLayer 並添加新的 Feature
            const vectorLayer = map.value.getLayers().getArray().find(layer => layer instanceof VectorLayer);
            const vectorSource = vectorLayer.getSource().getSource(); // 獲取 clusterSource 的原始 source
            vectorSource.addFeature(newFeature);

            const coordinates = fromLonLat([newSpot.px, newSpot.py]);
            map.value.getView().animate({
                center: coordinates,
                zoom: 17,
                duration: 1000
            });

            // 發送請求到後端
            axios({
                method: 'post',
                url: './api/Spot/Add',
                data: result.value
            }).then((response) => {
                console.log('新增成功');
                Swal.fire({
                    title: '新增成功',
                    imageUrl: '../我就讚.png',
                    imageWidth: 300,
                    imageHeight: 300,
                    confirmButtonText: '確定'
                });

                // 更新新標記的ID
                const newSpotId = response.data.id;
                //更新Feature的唯一標示符
                newFeature.setId(newSpotId);
                //更新Feature內的id屬性
                newFeature.set('id', newSpotId);
            }).catch(() => {
                console.log('329', result.value);
                Swal.fire({
                    title: '錯誤',
                    text: '無法新增景點，請稍後再試',
                    icon: 'error',
                    confirmButtonText: '確定'
                });
            });
        }
    });
}


onMounted(() => {
    getSpotApi();
});

</script>

<style scoped>
.map-container {
    width: 100%;
    height: 700px;
    border-radius: 20px;
    overflow: hidden;
    margin-top: 40px;
}

.mouse-position {
    bottom: 90px;
    background: white;
    border-radius: 10px;
    font-family: Arial, sans-serif;
    font-size: 15px;
    color: black;
    font-weight: bold;
}
</style>