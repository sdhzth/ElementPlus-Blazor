using Microsoft.JSInterop;
using System;
using System.Text;
using System.Threading.Tasks;

namespace ElementPlus.Blazor.Demo.Shared
{
    /// <summary>
    /// 前端登录态服务（演示用，纯客户端、无后端校验）。
    /// <list type="bullet">
    ///   <item>登录成功：生成一段「header.payload.signature」风格的模拟 token，写入 cookie <c>auth_token</c>（默认 1 天）。</item>
    ///   <item>路由守卫：未登录访问后台页时被 <see cref="Layout.MainLayout"/> 重定向到 <c>/login</c>。</item>
    ///   <item>退出：清除 cookie，token 即失效。</item>
    /// </list>
    /// <para>投产时把 <see cref="LoginAsync"/> 换成你的后端登录接口、由服务端签发真实 JWT 即可，其余逻辑不变。</para>
    /// </summary>
    public class AuthService
    {
        private readonly IJSRuntime _js;

        public AuthService(IJSRuntime js)
        {
            _js = js;
        }

        /// <summary>是否已登录（cookie 中存在非空 <c>auth_token</c>）。</summary>
        public async Task<bool> IsLoggedInAsync()
        {
            try
            {
                var token = await _js.InvokeAsync<string>("appAuth.getToken");
                return !string.IsNullOrEmpty(token);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 模拟登录：生成一段模拟 JWT 写入 cookie。
        /// 这里没有后端，token 是本地伪造的「样子货」——仅用于演示守卫与持久化流程。
        /// </summary>
        public async Task LoginAsync(string userName)
        {
            var name = string.IsNullOrWhiteSpace(userName) ? "admin" : userName;
            var now = DateTimeOffset.UtcNow;
            var header = Base64Url("{\"alg\":\"HS256\",\"typ\":\"JWT\"}");
            var payload = Base64Url($"{{\"sub\":\"{name}\",\"name\":\"{name}\",\"iat\":{now.ToUnixTimeSeconds()},\"exp\":{now.AddDays(1).ToUnixTimeSeconds()}}}");
            var signature = Base64Url(Guid.NewGuid().ToString());
            var token = $"{header}.{payload}.{signature}";

            await _js.InvokeVoidAsync("appAuth.setToken", token, 1);
        }

        /// <summary>退出登录：清除 cookie 中的 token。</summary>
        public async Task LogoutAsync()
        {
            await _js.InvokeVoidAsync("appAuth.clearToken");
        }

        /// <summary>Base64URL 编码（去 padding、+ → -、/ → _），避免 cookie 值里出现特殊字符。</summary>
        private static string Base64Url(string s)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(s))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
