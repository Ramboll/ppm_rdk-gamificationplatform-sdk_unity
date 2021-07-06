using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;

namespace GamificationBackend
{
    public partial class PlatformManager
    {
        public class PlatformApi
        {
            private readonly string _gameToken;
            private readonly string _authURL;
            private readonly string _identifyURL;
            private readonly string _activityURL;
            private readonly string _registerURL;
            private readonly string _filesListURL;
            private readonly string _fileContentURL;
            private readonly string _udfValueUrl;
            private readonly string _noticesUrl;
            private readonly string _observationsUrl;
            private readonly string _observationContentUrl;
            
            private string _personalToken;
            
            public PlatformApi(string host, string gameToken)
            {
                var prefix = $"{host}/gamification/api";
                _gameToken = gameToken;
                _authURL = $"{prefix}/auth/";
                _identifyURL = $"{prefix}/game/identify/";
                _activityURL = $"{prefix}/game/<game_id>/activity/<campaign_id>/";
                _registerURL = $"{prefix}/register/";
                _filesListURL = $"{prefix}/game/<game_id>/assets/<campaign_id>/";
                _fileContentURL = $"{prefix}/game-asset/<pk>/content/";
                _udfValueUrl = $"{prefix}/game/<game_id>/custom-fields/<campaign_id>/";
                _noticesUrl = $"{prefix}/game-notice/";
                _observationsUrl = $"{prefix}/safety-observation/";
                _observationContentUrl = $"{prefix}/safety-observation/<observation_id>/attachment";
            }
            
            #region API calls
            
            public IEnumerator BuildSession(string phone, string passwd, Action<PlatformResponse<PlaySession>> callback)
            {
                var requestPayload = new PayloadAuth
                {
                    game_token = _gameToken,
                    password = passwd,
                    phone = phone
                };
                var gameTokenPayload = new PayloadGameToken
                {
                    game_token = _gameToken
                };
                yield return PostJsonData<PayloadAuth, PayloadToken>(_authURL, requestPayload);
                var tokenResponse = (PlatformResponse<PayloadToken>) responseCache;
                if (tokenResponse.status == RequestStatus.ERROR)
                {
                    callback(new PlatformResponse<PlaySession>
                    {
                        content = default,
                        error = tokenResponse.error,
                        status = RequestStatus.ERROR
                    });
                    yield break;
                }
                
                _personalToken = tokenResponse.content.token;
                yield return PostJsonData<PayloadGameToken, PayloadGameDetail>(_identifyURL, gameTokenPayload);
                var gameDetail = (PlatformResponse<PayloadGameDetail>) responseCache;
                if (gameDetail.status == RequestStatus.ERROR)
                {
                    callback(new PlatformResponse<PlaySession>
                    {
                        content = default,
                        error = gameDetail.error,
                        status = RequestStatus.ERROR
                    });
                    yield break;
                }
                
                var url = _activityURL
                    .Replace("<game_id>", gameDetail.content.id.ToString())
                    .Replace("<campaign_id>", gameDetail.content.campaign.ToString());
                yield return GetJsonData<PayloadActivityDetail>(url);
                var activityResponse = (PlatformResponse<PayloadActivityDetail>) responseCache;
                if (activityResponse.status == RequestStatus.ERROR)
                {
                    callback(new PlatformResponse<PlaySession>
                    {
                        content = default,
                        error = activityResponse.error,
                        status = RequestStatus.ERROR
                    });
                    yield break;
                }
                
                PlaySession session = new PlaySession
                {
                    status = activityResponse.content.status,
                    appToken = _gameToken,
                    campaignID = gameDetail.content.campaign,
                    gameID = gameDetail.content.id,
                    personalToken = tokenResponse.content.token,
                    player = activityResponse.content.player
                };
                callback(new PlatformResponse<PlaySession>
                {
                    content = session,
                    error = "",
                    status = RequestStatus.SUCCESS
                });
            }

            public IEnumerator GetActivityStatus(PlaySession session, Action<PlatformResponse<PayloadActivityDetail>> callback)
            {
                var url = _activityURL
                    .Replace("<game_id>", session.gameID.ToString())
                    .Replace("<campaign_id>", session.campaignID.ToString());
                
                yield return GetJsonData<PayloadActivityDetail>(url);
                var activityResponse = (PlatformResponse<PayloadActivityDetail>) responseCache;
                session.status = activityResponse.content.status;
                callback(activityResponse);
            }

            public IEnumerator UpdateActivityStatus(PlaySession session, int status, Action<PlatformResponse<bool>> callback)
            {
                var url = _activityURL
                    .Replace("<game_id>", session.gameID.ToString())
                    .Replace("<campaign_id>", session.campaignID.ToString());
                var payload = new PayloadActivityDetail
                {
                    game = session.gameID,
                    campaign = session.campaignID,
                    last_modified = "",
                    player = session.player,
                    status = status
                };
                yield return PostJsonData<PayloadActivityDetail, PayloadActivityDetail>(url, payload);
                var activityResponse = (PlatformResponse<PayloadActivityDetail>) responseCache;
                if (activityResponse.status == RequestStatus.ERROR)
                {
                    callback(new PlatformResponse<bool>
                    {
                        content = default,
                        error = activityResponse.error,
                        status = RequestStatus.ERROR
                    });
                    yield break;
                }
                
                session.status = activityResponse.content.status;
                callback(new PlatformResponse<bool>
                {
                    content = activityResponse.content.status == status,
                    error = "",
                    status = RequestStatus.SUCCESS
                });
            }

            public IEnumerator RegisterPlayer(string firstName, string lastName,
                string company, string phone, string password, Action<PlatformResponse<bool>> callback)
            {
                var payload = new PayloadRegisterPlayer
                {
                    game_token = _gameToken,
                    first_name = firstName,
                    last_name = lastName,
                    company = company,
                    phone = phone,
                    password = password
                };
                yield return PostJsonData<PayloadRegisterPlayer, PayloadPlayer>(_registerURL, payload);
                var playerRegistrationResponse = (PlatformResponse<PayloadPlayer>) responseCache;
                if (playerRegistrationResponse.status == RequestStatus.ERROR)
                {
                    callback(new PlatformResponse<bool>
                    {
                        content = default,
                        error = playerRegistrationResponse.error,
                        status = RequestStatus.ERROR
                    });
                    yield break;
                }
                
                callback(new PlatformResponse<bool>
                {
                    content = playerRegistrationResponse.content.id > 0,
                    error = "",
                    status = RequestStatus.SUCCESS
                });
            }

            public IEnumerator GetFilesList(PlaySession session, Action<PlatformResponseMany<GameAsset>> callback)
            {
                var url = _filesListURL
                    .Replace("<game_id>", session.gameID.ToString())
                    .Replace("<campaign_id>", session.campaignID.ToString());
                
                yield return GetJsonData<GameAsset>(url, true);
                var filesResponse = (PlatformResponseMany<GameAsset>) responseCache;
                callback(filesResponse);
            }

            public IEnumerator GetFileContent(int assetId, Action<AssetData> callback)
            {
                var url = _fileContentURL
                    .Replace("<pk>", assetId.ToString());

                yield return GetFileData(url);
                callback((AssetData)responseCache);
            }

            public IEnumerator SetUdfFieldValue<T>(PlaySession session, string name, T value, int udfType,
                Action<PlatformResponseMany<UdfValue>> callback)
            {
                var url = _udfValueUrl
                    .Replace("<game_id>", session.gameID.ToString())
                    .Replace("<campaign_id>", session.campaignID.ToString());
                PayloadSetUdfValue<T> payload = new PayloadSetUdfValue<T>
                {
                    name = name,
                    value = value,
                    type_id = udfType
                };
                
                yield return PostJsonData<PayloadSetUdfValue<T>, PayloadUdfValue>(url, payload, true);
                var udfValueResponse = (PlatformResponseMany<PayloadUdfValue>) responseCache;

                var response = new PlatformResponseMany<UdfValue>
                {
                    error = "",
                    status = RequestStatus.SUCCESS,
                    content = udfValueResponse.content.Select(x => new UdfValue
                    {
                        id = x.id,
                        name = x.field_type.name,
                        type_id = x.field_type.type_id,
                        target_object_id = x.target_object_id,
                        value = x.value
                    }).ToList()
                };
                callback(response);
            }

            public IEnumerator GetNotices(Action<PlatformResponseMany<Notice>> callback)
            {
                var url = _noticesUrl;
                yield return GetJsonData<Notice>(url, true);
                var noticesResponse = (PlatformResponseMany<Notice>) responseCache;
                callback(noticesResponse);
            }

            public IEnumerator CreateObservation(PlaySession session, string title, string note, int obsType,
                Action<PlatformResponse<Observation>> callback)
            {
                var url = _observationsUrl;
                PayloadObservation payload = new PayloadObservation
                {
                    mobile_app = session.gameID,
                    note = note,
                    obs_type = obsType,
                    scope = session.campaignID,
                    status = 10,  // status = SUBMITTED
                    title = title
                };
                yield return PostJsonData<PayloadObservation, Observation>(url, payload);
                var observationResponse = (PlatformResponse<Observation>) responseCache;
                callback(observationResponse);
            }

            public IEnumerator AttachFileToObservation(PlaySession session, Observation observation, byte[] attachment,
                Action<PlatformResponse<Observation>> callback)
            {
                var url = _observationContentUrl;
                // yield return PostFileData<PayloadObservation, Observation>(url, payload);
                // var observationResponse = (PlatformResponse<Observation>) responseCache;
                // callback(observationResponse);
                yield break;
            }
            
            #endregion

            #region Http handling
            
            private object responseCache;

            private T Deserialize<T>(string payload)
            where T : IBaseSerializable
            {
                var result = JsonUtility.FromJson<T>(payload);
                if (!result.check())
                {
                    throw new DeserializeException();
                }

                return result;
            }
            
            private IEnumerator PostJsonData<T1, T2>(string url, T1 payload, bool expectMany=false)
            where T1: IBaseSerializable
            where T2 : IBaseSerializable, new()
            {
                using (UnityWebRequest webRequest = UnityWebRequest.Put(url, JsonUtility.ToJson(payload)))
                {
                    webRequest.method = "POST";
                    webRequest.SetRequestHeader("Accept", "application/json");
                    webRequest.SetRequestHeader("Content-Type", "application/json");
                    if (!string.IsNullOrEmpty(_personalToken))
                    {
                        webRequest.SetRequestHeader("Authorization", "Token " + _personalToken);
                    }
                    
                    yield return webRequest.SendWebRequest();

                    if (webRequest.isNetworkError)
                    {
                        responseCache = new PlatformResponse<T2>
                        {
                            status = RequestStatus.ERROR,
                            error = "Could not connect to platform API",
                            content = default
                        };
                    } else if (!string.IsNullOrEmpty(webRequest.error))
                    {
                        if (expectMany)
                        {
                            responseCache = new PlatformResponseMany<T2>
                            {
                                status = RequestStatus.ERROR,
                                error = "404 NOT FOUND: " + url,
                                content = default
                            };
                        }
                        else
                        {
                            responseCache = new PlatformResponse<T2>
                            {
                                status = RequestStatus.ERROR,
                                error = "404 NOT FOUND: " + url,
                                content = default
                            };
                        }
                    }
                    else
                    {
                        try
                        {
                            if (expectMany)
                            {
                                List<T2> list = new List<T2>();
                                var N = JSON.Parse("{\"results\":" + webRequest.downloadHandler.text + "}");
                                for (int i = 0; i<N["results"].Count; i++)
                                {
                                    
                                    list.Add((T2)new T2().Create(N["results"][i]));
                                }

                                responseCache = new PlatformResponseMany<T2>
                                {
                                    status = RequestStatus.SUCCESS,
                                    error = "",
                                    content = list
                                };
                            }
                            else
                            {
                                responseCache = new PlatformResponse<T2>
                                {
                                    status = RequestStatus.SUCCESS,
                                    error = "",
                                    content = Deserialize<T2>(webRequest.downloadHandler.text)
                                };
                            }
                        }
                        catch (DeserializeException)
                        {
                            Debug.LogError("Response was: " + webRequest.downloadHandler.text);
                            responseCache = new PlatformResponse<T2>
                            {
                                status = RequestStatus.ERROR,
                                error = "Response did not match expected format",
                                content = default
                            };
                        }
                        catch (ArgumentException)
                        {
                            responseCache = new PlatformResponse<T2>
                            {
                                status = RequestStatus.ERROR,
                                error = "Response did not match expected format",
                                content = default
                            };
                        }
                    }
                }
            }
            
            private IEnumerator GetJsonData<T2>(string url, bool expectMany = false)
            where T2 : IBaseSerializable, new()
            {
                using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
                {
                    webRequest.SetRequestHeader("Accept", "application/json");
                    // webRequest.SetRequestHeader("Content-Type", "application/json");
                    if (!string.IsNullOrEmpty(_personalToken))
                    {
                        webRequest.SetRequestHeader("Authorization", "Token " + _personalToken);
                    }
                    
                    yield return webRequest.SendWebRequest();

                    if (webRequest.isNetworkError)
                    {
                        responseCache = new PlatformResponse<T2>
                        {
                            status = RequestStatus.ERROR,
                            error = "Could not connect to platform API",
                            content = default
                        };
                    } else if (!string.IsNullOrEmpty(webRequest.error))
                    {
                        if (expectMany)
                        {
                            responseCache = new PlatformResponseMany<T2>
                            {
                                status = RequestStatus.ERROR,
                                error = "404 NOT FOUND: " + url,
                                content = default
                            };
                        }
                        else
                        {
                            responseCache = new PlatformResponse<T2>
                            {
                                status = RequestStatus.ERROR,
                                error = "404 NOT FOUND: " + url,
                                content = default
                            };
                        }
                    }
                    else
                    {
                        try
                        {
                            if (expectMany)
                            {
                                List<T2> list = new List<T2>();
                                var N = JSON.Parse("{\"results\":" + webRequest.downloadHandler.text + "}");
                                for (int i = 0; i<N["results"].Count; i++)
                                {
                                    
                                    list.Add((T2)new T2().Create(N["results"][i]));
                                }

                                responseCache = new PlatformResponseMany<T2>
                                {
                                    status = RequestStatus.SUCCESS,
                                    error = "",
                                    content = list
                                };
                            }
                            else
                            {
                                responseCache = new PlatformResponse<T2>
                                {
                                    status = RequestStatus.SUCCESS,
                                    error = "",
                                    content = Deserialize<T2>(webRequest.downloadHandler.text)
                                };
                            }
                        }
                        catch (DeserializeException)
                        {
                            Debug.LogError("Response was: " + webRequest.downloadHandler.text);
                            responseCache = new PlatformResponse<T2>
                            {
                                status = RequestStatus.ERROR,
                                error = "Response did not match expected format",
                                content = default
                            };
                        }
                        catch (ArgumentException)
                        {
                            responseCache = new PlatformResponse<T2>
                            {
                                status = RequestStatus.ERROR,
                                error = "Response did not match expected format",
                                content = default
                            };
                        }
                    }
                }
            }

            /// <summary>
            /// Get's file data from URL into a byte array
            /// </summary>
            /// <param name="url">The URL to get the data from</param>
            /// <returns></returns>
            private IEnumerator GetFileData(string url)
            {
                var www = UnityWebRequest.Get(url);
                if (!string.IsNullOrEmpty(_personalToken))
                {
                    www.SetRequestHeader("Authorization", "Token " + _personalToken);
                }

                yield return www.SendWebRequest();

                responseCache = new AssetData
                {
                    byteData = www.downloadHandler.data,
                    mimeType = www.GetResponseHeader("Content-Type")
                };
            }
            
            #endregion
        }

        public enum RequestStatus { ERROR, SUCCESS }

        interface IPlatformResponse
        {
            
        }
        public class PlatformResponse<T> : IPlatformResponse
        {
            public RequestStatus status;
            public string error = "";
            public T content;
        }

        public class PlatformResponseMany<T> : IPlatformResponse
        {
            public RequestStatus status;
            public string error = "";
            public List<T> content;
        }

        public class AssetData
        {
            public byte[] byteData;
            public string mimeType;
        }
    }
}