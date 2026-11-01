// 前端登录态：把模拟 token 存进 cookie（替代方案是 localStorage，但 cookie 能随请求自动带上，更贴近真实鉴权）。
// 仅做演示用途——token 由前端伪造，真正的校验应放在后端。
window.appAuth = {
  setToken: function (token, days) {
    var d = days || 1;
    var expires = new Date(Date.now() + d * 24 * 60 * 60 * 1000).toUTCString();
    document.cookie = 'auth_token=' + encodeURIComponent(token) +
      '; path=/; expires=' + expires + '; SameSite=Lax';
  },
  getToken: function () {
    var m = document.cookie.match(/(?:^|;\s*)auth_token=([^;]*)/);
    return m ? decodeURIComponent(m[1]) : '';
  },
  clearToken: function () {
    document.cookie = 'auth_token=; path=/; expires=Thu, 01 Jan 1970 00:00:00 GMT; SameSite=Lax';
  }
};
