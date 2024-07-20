import { defineStore } from "pinia";

export const useCardStore = defineStore("CardLayer", {
    state: () => ({
        ListAction: false,
        DownloadAction: false,
        UpFileAction: false,
        RenewCardAct: false,
    }),
    actions: {
        ListCard() {
            this.ListAction = !this.ListAction;
            if (this.ListAction) {
                this.DownloadAction = false;
                this.UpFileAction = false;
            }
        },
        DownloadCard() {
            this.DownloadAction = !this.DownloadAction;
            if (this.DownloadAction) {
                this.ListAction = false;
                this.UpFileAction = false;
            }
        },
        UpFileCard() {
            this.UpFileAction = !this.UpFileAction;
            if (this.UpFileAction) {
                this.ListAction = false;
                this.DownloadAction = false;
            }
        },
        RenewListCard() {
            this.RenewCardAct = !this.RenewCardAct;
        },
    },
});
