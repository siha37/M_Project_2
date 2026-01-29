using System;
using System.Threading.Tasks;
using MyFolder._1._Scripts._9._Vivox;
using Steamworks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;

namespace MyFolder._1._Scripts._4._Network
{
    /// <summary>
    /// 스팀 SDK를 통한 Unity Authentication 및 네트워크 상태 관리
    /// </summary>
    public class SteamAccount : MonoBehaviour
    {
        Callback<GetTicketForWebApiResponse_t> m_AuthTicketForWebApiResponseCallback;
        string m_SessionTicket;
        private string identity = "unityauthenticationservice";
        static HAuthTicket authTicket;
        
        [Header("테스트 설정")]
        [SerializeField] private bool enableTestMode = true; // 테스트 모드 활성화

        public void Start()
        {
            _=InitializeUnityServices();
            SignInWithSteam();
        }

        private async Task InitializeUnityServices()
        {
            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    Debug.Log("Unity Services 초기화 시작...");
                    await UnityServices.InitializeAsync();
                    Debug.Log("Unity Services 초기화 완료");
                }
                else
                {
                    Debug.Log("Unity Services는 이미 초기화되었습니다.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Unity Services 초기화 실패: {ex.Message}");
                
                NetworkStateManager.Instance.ChangeState(NetworkState.Disconnected, "서비스 초기화 실패");
            }
        }
        
        void SignInWithSteam()
        {
            // It's not necessary to add event handlers if they are 
            // already hooked up.
            // Callback.Create return value must be assigned to a 
            // member variable to prevent the GC from cleaning it up.
            // Create the callback to receive events when the session ticket
            // is ready to use in the web API.
            // See GetAuthSessionTicket document for details.

            
            
            if (m_AuthTicketForWebApiResponseCallback != null)
            {
                m_AuthTicketForWebApiResponseCallback.Dispose();
                m_AuthTicketForWebApiResponseCallback = null;
            }
            
            if (AuthenticationService.Instance.IsSignedIn)
            {
                NetworkStateManager.Instance.ChangeState(NetworkState.Connected, "이미 연결되어 있음");
                return;
            }
            
            NetworkStateManager.Instance.ChangeState(NetworkState.Authenticating, "스팀 인증 시도");
            if (!SteamManager.Initialized)
            {
                Debug.LogWarning("스팀이 초기화되지 않음");
                if (enableTestMode)
                {
                    Debug.LogWarning("테스트 모드: 스팀 없이 강제 인증 진행");
                    _ = ForceAuthentication();
                    return;
                }
                else
                {
                    NetworkStateManager.Instance.ChangeState(NetworkState.Disconnected, "스팀 초기화 실패");
                    return;
                }
            }
            
            m_AuthTicketForWebApiResponseCallback = Callback<GetTicketForWebApiResponse_t>.Create(OnAuthCallback);

            SteamUser.GetAuthTicketForWebApi(identity);
        }

        void OnAuthCallback(GetTicketForWebApiResponse_t callback)
        {
            m_SessionTicket = BitConverter.ToString(callback.m_rgubTicket).Replace("-", string.Empty);
            m_AuthTicketForWebApiResponseCallback.Dispose();
            m_AuthTicketForWebApiResponseCallback = null;
            Debug.Log("Steam Login success. Session Ticket: " + m_SessionTicket);
            // Call Unity Authentication SDK to sign in or link with Steam, displayed in the following examples, using the same identity string and the m_SessionTicket.

            _=SignInWithSteamAsync(m_SessionTicket, identity);
        }

        
        async Task SignInWithSteamAsync(string ticket, string identity)
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                NetworkStateManager.Instance.ChangeState(NetworkState.Connected, "이미 연결되어 있음");
                return;
            }
            try
            {
                // 스팀 인증 과정
                
                // 인증 서비스 로그인
                await AuthenticationService.Instance.SignInWithSteamAsync(ticket, identity);
                
                // vivox 초기화
                await VivoxService.Instance.InitializeAsync();
                VivoxManager.Instance.LoginToVivoxAsync();
                
                // 상태 변경
                NetworkStateManager.Instance.ChangeState(NetworkState.Connected, "스팀 로그인 성공");
                Debug.Log("SignIn is successful.");
                NetworkStateManager.Instance.SetUserId(AuthenticationService.Instance.PlayerId);
            }
            catch (AuthenticationException ex)
            {
                Debug.LogException(ex);
                
                if (enableTestMode)
                {
                    Debug.LogWarning("테스트 모드: 스팀 인증 실패했지만 강제 진행");
                    _ = ForceAuthentication();
                }
                else
                {
                    NetworkStateManager.Instance.ChangeState(NetworkState.Disconnected, "스팀 로그인 인증 에러");
                    NetworkStateManager.Instance.SetError($"스팀 인증 실패: {ex.Message}");
                }
            }
            catch (RequestFailedException ex)
            {
                Debug.LogException(ex);
                
                if (enableTestMode)
                {
                    Debug.LogWarning("테스트 모드: 스팀 로그인 요청 실패했지만 강제 진행");
                    _ = ForceAuthentication();
                }
                else
                {
                    NetworkStateManager.Instance.ChangeState(NetworkState.Disconnected, "스팀 로그인 요청 에러");
                    NetworkStateManager.Instance.SetError($"스팀 로그인 요청 실패: {ex.Message}");
                }
            }
        }
        
     
        async Task UnlinkSteamAsync()
        {
            try
            {
                await AuthenticationService.Instance.UnlinkSteamAsync();
                Debug.Log("Unlink is successful.");
            }
            catch (AuthenticationException ex)
            {
                // Compare error code to AuthenticationErrorCodes
                // Notify the player with the proper error message
                Debug.LogException(ex);
            }
            catch (RequestFailedException ex)
            {
                // Compare error code to CommonErrorCodes
                // Notify the player with the proper error message
                Debug.LogException(ex);
            }
        }
        /// <summary>
        /// 테스트용 강제 인증 메서드
        /// </summary>
        private async Task ForceAuthentication()
        {
            try
            {
                Debug.Log("테스트 모드: 강제 인증 시도 중...");
                
                
                //테스트 인증 과정
                
                // Unity Anonymous 인증으로 대체
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                
                
                // 테스트용 가짜 사용자 ID 설정
                string testUserId = $"test_user_{UnityEngine.Random.Range(1000, 9999)}";
                NetworkStateManager.Instance.SetUserId(AuthenticationService.Instance.PlayerId);
                NetworkStateManager.Instance.ChangeState(NetworkState.Connected, "테스트 모드 강제 인증 성공");
                
                
                // vivox 초기화
                await VivoxService.Instance.InitializeAsync();
                VivoxManager.Instance.LoginToVivoxAsync();
                
                Debug.Log($"테스트 모드 강제 인증 완료. 사용자 ID: {testUserId}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"강제 인증도 실패: {ex.Message}");
                NetworkStateManager.Instance.ChangeState(NetworkState.Disconnected, "강제 인증 실패");
            }
        }
    }
    
}