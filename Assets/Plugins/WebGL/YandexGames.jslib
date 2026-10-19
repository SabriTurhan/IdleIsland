// Yandex Games SDK bridge. The page (WebGLTemplates/IslandEscape/index.html) initialises the SDK before
// Unity starts and keeps the results in window.yg (sdk, player, payments, catalog, purchases, cloud save).
// Async results come back through window.yg.send(method, value) -> SendMessage("YandexBridge", ...).
var YandexGamesLib = {
    $ygString: function (value) {
        var text = value === undefined || value === null ? "" : String(value);
        var size = lengthBytesUTF8(text) + 1;
        var buffer = _malloc(size);
        stringToUTF8(text, buffer, size);
        return buffer;
    },

    YG_IsAvailable: function () {
        return window.yg && window.yg.sdk ? 1 : 0;
    },

    YG_GetLanguage: function () {
        var sdk = window.yg && window.yg.sdk;
        var lang = sdk && sdk.environment && sdk.environment.i18n ? sdk.environment.i18n.lang : "";
        return ygString(lang);
    },

    YG_GetDeviceType: function () {
        var sdk = window.yg && window.yg.sdk;
        return ygString(sdk && sdk.deviceInfo ? sdk.deviceInfo.type : "");
    },

    YG_Ready: function () {
        var sdk = window.yg && window.yg.sdk;
        if (sdk && sdk.features && sdk.features.LoadingAPI) sdk.features.LoadingAPI.ready();
    },

    YG_GameplayStart: function () {
        var sdk = window.yg && window.yg.sdk;
        if (sdk && sdk.features && sdk.features.GameplayAPI) sdk.features.GameplayAPI.start();
    },

    YG_GameplayStop: function () {
        var sdk = window.yg && window.yg.sdk;
        if (sdk && sdk.features && sdk.features.GameplayAPI) sdk.features.GameplayAPI.stop();
    },

    YG_ShowFullscreen: function () {
        var sdk = window.yg && window.yg.sdk;
        if (!sdk) { window.yg.send("OnFullscreenClosed", "0"); return; }
        var done = false;
        var finish = function (shown) { if (done) return; done = true; window.yg.send("OnFullscreenClosed", shown ? "1" : "0"); };
        sdk.adv.showFullscreenAdv({ callbacks: {
            onOpen: function () { window.yg.send("OnAdOpened", ""); },
            onClose: function (wasShown) { finish(wasShown); },
            onError: function () { finish(false); }
        }});
    },

    YG_ShowRewarded: function () {
        var sdk = window.yg && window.yg.sdk;
        if (!sdk) { window.yg.send("OnRewardedClosed", "0"); return; }
        var done = false, rewarded = false;
        var finish = function () { if (done) return; done = true; window.yg.send("OnRewardedClosed", rewarded ? "1" : "0"); };
        sdk.adv.showRewardedVideo({ callbacks: {
            onOpen: function () { window.yg.send("OnAdOpened", ""); },
            onRewarded: function () { rewarded = true; },
            onClose: function () { finish(); },
            onError: function () { finish(); }
        }});
    },

    YG_GetCatalog: function () {
        return ygString(window.yg ? JSON.stringify(window.yg.catalog || []) : "[]");
    },

    YG_GetPurchases: function () {
        return ygString(window.yg ? JSON.stringify(window.yg.purchases || []) : "[]");
    },

    YG_Purchase: function (idPtr) {
        var id = UTF8ToString(idPtr);
        var payments = window.yg && window.yg.payments;
        if (!payments) { window.yg.send("OnPurchaseFailed", id); return; }
        payments.purchase({ id: id }).then(function (purchase) {
            window.yg.send("OnPurchaseSucceeded", JSON.stringify({ productID: purchase.productID, purchaseToken: purchase.purchaseToken }));
        }).catch(function () {
            window.yg.send("OnPurchaseFailed", id);
        });
    },

    YG_Consume: function (tokenPtr) {
        var token = UTF8ToString(tokenPtr);
        var payments = window.yg && window.yg.payments;
        if (payments) payments.consumePurchase(token).catch(function (e) { console.warn("[YG] consume failed", e); });
    },

    YG_CloudLoad: function () {
        return ygString(window.yg && window.yg.cloud ? JSON.stringify(window.yg.cloud) : "");
    },

    YG_CloudSave: function (jsonPtr) {
        if (window.yg && window.yg.cloudSave) window.yg.cloudSave(UTF8ToString(jsonPtr));
    }
};

autoAddDeps(YandexGamesLib, "$ygString");
mergeInto(LibraryManager.library, YandexGamesLib);
