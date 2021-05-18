using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;

namespace GamificationBackend
{
    class DeserializeException : ArgumentException {}

    public interface IBaseSerializable
    {
        bool check();

        IBaseSerializable Create(JSONNode data);
    }
    
    /// <summary>
    /// An instance describing the authenticated session. Inpect the IsValid property to learn
    /// whether it has authenticated correctly
    /// </summary>
    [Serializable]
    public class PlaySession : IBaseSerializable
    {
        public string appToken;
        public string personalToken;
        public int campaignID;
        public int gameID;
        public int status;
        public int player;
        
        public bool check()
        {
            return player != 0;
        }

        public IBaseSerializable Create(JSONNode data)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class PayloadAuth : IBaseSerializable
    {
        public string game_token;
        public string phone;
        public string password;
        
        public bool check()
        {
            return !string.IsNullOrEmpty(phone);
        }

        public IBaseSerializable Create(JSONNode data)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class PayloadToken : IBaseSerializable
    {
        public string token;
        
        public bool check()
        {
            return !string.IsNullOrEmpty(token);
        }

        public IBaseSerializable Create(JSONNode data)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class PayloadGameToken : IBaseSerializable
    {
        public string game_token;
        
        public bool check()
        {
            return !string.IsNullOrEmpty(game_token);
        }

        public IBaseSerializable Create(JSONNode data)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class PayloadGameDetail : IBaseSerializable
    {
        public int id;
        public string name;
        public string content_url;
        public int status;
        public int campaign;
        
        public bool check()
        {
            return id != 0;
        }

        public IBaseSerializable Create(JSONNode data)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class PayloadActivityDetail : IBaseSerializable
    {
        public int game;
        public int player;
        public int status;
        public int campaign;
        public string last_modified;
        
        public bool check()
        {
            return game != 0;
        }

        public IBaseSerializable Create(JSONNode data)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class PayloadRegisterPlayer : IBaseSerializable
    {
        public string first_name;
        public string last_name;
        public string company;
        public string phone;
        public string password;
        public string game_token;
        
        public bool check()
        {
            return !string.IsNullOrEmpty(phone);
        }

        public IBaseSerializable Create(JSONNode data)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class PayloadPlayer : IBaseSerializable
    {
        public int id;
        public string username;
        public string email;
        public string first_name;
        public string last_name;
        public string full_name;
        
        public bool check()
        {
            return id != 0;
        }

        public IBaseSerializable Create(JSONNode data)
        {
            throw new NotImplementedException();
        }
    }

    [Serializable]
    public class GameAsset : IBaseSerializable
    {
        public int id;
        public string slug;
        public int game;
        public int campaign;
        public int status;
        public int asset_type;
        public int category;
        public string file_name;
        
        public bool check()
        {
            return id != 0;
        }

        public override string ToString()
        {
            return $"{id}::{slug}::{game}::{campaign}::{asset_type}::{file_name}";
        }

        public IBaseSerializable Create(JSONNode data)
        {
            var result =  new GameAsset
            {
                id = data["id"],
                slug = data["slug"],
                game = data["game"],
                campaign = data["campaign"],
                status = data["status"],
                category = data["category"],
                asset_type = data["asset_type"],
                file_name = data["file_name"],
            };
            return result;
        }
    }

    [Serializable]
    public class Notice : IBaseSerializable
    {
        public int id;
        public int campaign;
        public string title;
        public string content;
        public string date;
        public string originator_name;
        
        public bool check()
        {
            return id != 0;
        }

        public IBaseSerializable Create(JSONNode data)
        {
            var result =  new Notice
            {
                id = data["id"],
                campaign = data["campaign"],
                title = data["title"],
                content = data["content"],
                date = data["date"],
                originator_name = data["originator_name"],
            };
            return result;
        }

        public override string ToString()
        {
            return $"{id}::{originator_name}::{date}";
        }
    }

    /// <summary>
    /// Data to send to API for setting values
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class PayloadSetUdfValue<T> : IBaseSerializable
    {
        public string name;
        public T value;
        public int type_id;
        
        public bool check()
        {
            return !string.IsNullOrEmpty(name);
        }

        public IBaseSerializable Create(JSONNode data)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Data received from the API when setting values
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class PayloadUdfValue : IBaseSerializable
    {
        // {"id":5005,"field_type":{"id":114,"name":"Game Score","type_id":20,"options":"","project":655},"value":7,"target_object_id":3}
        public int id;
        public UdfFieldType field_type;
        public string value;
        public int target_object_id;
        
        public bool check()
        {
            return id != 0;
        }

        public override string ToString()
        {
            return $"{id}::{field_type.name}::{value}::{field_type.type_id}";
        }

        public IBaseSerializable Create(JSONNode data)
        {
            var result =  new PayloadUdfValue
            {
                id = data["id"],
                field_type = new UdfFieldType
                {
                    name = data["field_type"]["name"],
                    type_id = data["field_type"]["type_id"],
                },
                target_object_id = data["target_object_id"],
                value = data["value"]
            };
            return result;
        }
    }

    [Serializable]
    public class UdfFieldType
    {
        public int id;
        public string name;
        public int type_id;
    }

    [Serializable]
    public class UdfValue
    {
        public int id;
        public string name;
        public string value;
        public int type_id;
        public int target_object_id;
        
        public bool check()
        {
            return id != 0;
        }

        public override string ToString()
        {
            return $"{id}::{name}::{value}::{type_id}";
        }
    }

    public enum CompletionStatus
    {
        ENROLLED,
        STARTED,
        COMPLETED
    }

    public enum UdfType
    {
        TEXT_TYPE,
        NUMBER_TYPE,
        TEXT_LIST_TYPE,
        NUMBER_LIST_TYPE,
        DATE_TYPE,
        DATE_LIST_TYPE,
        LINK_TYPE,
        LINK_LIST_TYPE
    }
    
}
