// 启动时自动加载外部效果 helper（mos_灯光混合 等）
// EffectCraft 默认不会在启动时启动 helpers，这里手动扫描一次。
// 需要 Settings ▸ Scripting & Expressions ▸ Allow Scripts to Write Files and Access Network 处于开启状态。
(function () {
    try {
        app.executeCommand("effect.helpers.scan");
    } catch (e) {
        // 权限关闭或扫描失败时静默，用户仍可用 效果 ▸ 扫描效果辅助程序...
    }
})();
