//點擊MenuCom中的查詢地點，可以透過pinia設定布林值後在MainCom的watch進行監看布林值來決定是否進行查詢動作

import { defineStore } from 'pinia'

export const useSearchStore = defineStore('index', {
  state: () => ({
    searchAction: false,
    addAction: false
  }),
  actions: {
    SearchSpot() {
      this.searchAction = true
    },
    resetSearchSpot() {
      this.searchAction = false
    },
    AddSpot() {
      this.addAction = true
    },
    resetAddSpot() {
      this.addAction = false
    }
  }
})
