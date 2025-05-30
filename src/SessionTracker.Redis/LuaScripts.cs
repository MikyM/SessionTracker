namespace SessionTracker.Redis;

/// <summary>
/// The LUA scripts used by Redis.
/// </summary>
internal static class LuaScripts
{
    // We use Lua scripts to achieve fully atomic operations
 
    // KEYS[1] = = key
    // ARGV[1] = absolute-expiration - Unix timestamp in seconds as long (-1 for none)
    // ARGV[2] = sliding-expiration - number of seconds as long (-1 for none)
    // ARGV[3] = relative-expiration (long, in seconds, -1 for none) - Min(absolute-expiration - Now, sliding-expiration)
    // ARGV[4] = data - serialized JSON
    // this order should not change LUA script depends on it
    /// <summary>
    /// Script that HSET's the key's value with expiration data only if the key does not exist, returns existing value or "1" if successfully set.
    /// </summary>
    internal const string SetNotExistsAndReturnScript = (@"
                local result = redis.call('HGET', KEYS[1], 'data')
                if result ~= false then
                    return result
                end

                redis.call('HSET', KEYS[1], 'absexp', ARGV[1], 'sldexp', ARGV[2], 'data', ARGV[4])

                if ARGV[3] ~= '-1' then
                  redis.call('EXPIRE', KEYS[1], ARGV[3])
                end 

                return '1'");
    
    // KEYS[1] = = key
    // ARGV[1] = absolute-expiration - Unix timestamp in seconds as long (-1 for none)
    // ARGV[2] = sliding-expiration - number of seconds as long (-1 for none)
    // ARGV[3] = relative-expiration (long, in seconds, -1 for none) - Min(absolute-expiration - Now, sliding-expiration)
    // ARGV[4] = whether to return existing data if applies - 0 for no or 1 for yes
    // ARGV[5] = evicted key
    // this order should not change LUA script depends on it
    /// <summary>
    /// Script that DELS's the key only if it exists, also re-caches the DEL's key's value as an "evicted" entry, can return the just evicted value.
    /// </summary>
    internal const string RemoveMoveToEvictedScript = (@"
                local result = redis.call('HGET', KEYS[1], 'data')
                if result == false then
                  local evicted_result = redis.call('EXISTS', ARGV[5])
                  if evicted_result ~= 0 then
                     return '0'
                  end
                  return nil
                end

                redis.call('DEL', KEYS[1])

                redis.call('HSET', ARGV[5], 'absexp', ARGV[1], 'sldexp', ARGV[2], 'data', result)

                if ARGV[3] ~= '-1' then
                  redis.call('EXPIRE', ARGV[5], ARGV[3])
                end 

                if ARGV[4] == '1' then
                  return result
                end

                return '1'");
    
    // KEYS[1] = = evicted key
    // ARGV[1] = absolute-expiration - Unix timestamp in seconds as long (-1 for none)
    // ARGV[2] = sliding-expiration - number of seconds as long (-1 for none)
    // ARGV[3] = relative-expiration (long, in seconds, -1 for none) - Min(absolute-expiration - Now, sliding-expiration)
    // ARGV[4] = whether to return existing data if applies - 0 for no or 1 for yes
    // ARGV[5] = key
    // this order should not change LUA script depends on it
    /// <summary>
    /// Script that DELS's the key only if it exists in evicted cache, also re-caches the DEL's key's value as an "regular" entry, can return the just evicted value.
    /// </summary>
    internal const string RestoreMoveToRegularScript = (@"
                local result = redis.call('HGET', KEYS[1], 'data')
                if result == false then
                  local regular_result = redis.call('EXISTS', ARGV[5])
                  if regular_result ~= 0 then
                     return '0'
                  end
                  return nil
                end

                redis.call('DEL', KEYS[1])

                redis.call('HSET', ARGV[5], 'absexp', ARGV[1], 'sldexp', ARGV[2], 'data', result)

                if ARGV[3] ~= '-1' then
                  redis.call('EXPIRE', ARGV[5], ARGV[3])
                end 

                if ARGV[4] == '1' then
                  return result
                end

                return '1'");
    
    // KEYS[1] = = key
    // ARGV[1] = data - byte[]
    // ARGV[2] = whether to return post update data - 0 for no or 1 for yes
    // ARGV[3] = evicted key
    // this order should not change LUA script depends on it
    /// <summary>
    /// Script that HSET's the key's value only if it exists, refreshes the expiration, can return the just updated key's value.
    /// </summary>
    internal const string UpdateExistsAndRefreshConditionalReturnLastScript = (@"
                local sub = function (key)
                  local bulk = redis.call('HGETALL', key)
	                 local result = {}
	                 local nextkey
	                 for i, v in ipairs(bulk) do
		                 if i % 2 == 1 then
			                 nextkey = v
		                 else
			                 result[nextkey] = v
		                 end
	                 end
	                 return result
                end

                local result = sub(KEYS[1])

                if next(result) == nil then
                  local evicted_result = redis.call('EXISTS', ARGV[3])
                  if evicted_result ~= 0 then
                     return '0'
                  end
                  return nil
                end

                local sldexp = tonumber(result['sldexp'])
                if sldexp ~= -1 then
                  local exp = 1
                  local absexp = tonumber(result['absexp'])
                  local time = tonumber(redis.call('TIME')[1])
                  if absexp ~= -1 then
                    local relexp = absexp - time
                    if relexp <= sldexp then
                      exp = relexp
                    else
                      exp = sldexp                   
                    end
                  else
                    exp = sldexp
                  end
                  redis.call('EXPIRE', KEYS[1], exp, 'XX')
                end

                redis.call('HSET', KEYS[1], 'data', ARGV[1])
               
                if ARGV[2] == '1' then
                  return redis.call('HGET', KEYS[1], 'data')
                end
                return '1'");

    // KEYS[1] = = key
    // ARGV[1] = whether to return data or only refresh - 0 for no data, 1 to return data
    // ARGV[2] = evicted key
    // this order should not change LUA script depends on it
    /// <summary>
    /// Script that HGET's the key's value only if it exists, refreshes the expiration, can return the value or not.
    /// </summary>
    internal const string GetAndRefreshScript = (@"
                local sub = function (key)
                  local bulk = redis.call('HGETALL', key)
	                 local result = {}
	                 local nextkey
	                 for i, v in ipairs(bulk) do
		                 if i % 2 == 1 then
			                 nextkey = v
		                 else
			                 result[nextkey] = v
		                 end
	                 end
	                 return result
                end

                local result = sub(KEYS[1])

                if next(result) == nil then
                  local evicted_result = redis.call('EXISTS', ARGV[2])
                  if evicted_result ~= 0 then
                     return '0'
                  end
                  return nil
                end

                local sldexp = tonumber(result['sldexp'])
                local absexp = tonumber(result['absexp'])

                if sldexp == -1 then
                  if ARGV[1] == '1' then
                    return result['data']
                  else
                    return '1'
                  end
                end

                local exp = 1
                local time = tonumber(redis.call('TIME')[1])
                if absexp ~= -1 then
                  local relexp = absexp - time
                  if relexp <= sldexp then
                    exp = relexp
                  else
                    exp = sldexp                   
                  end
                else
                  exp = sldexp
                end
                
                redis.call('EXPIRE', KEYS[1], exp, 'XX')
                                
                if ARGV[1] == '1' then
                  return result['data']
                end
                return '1'");
    
    // KEYS[1] = = evicted key
    // ARGV[1] = whether to return existing data if applies - 0 for no or 1 for yes
    // ARGV[2] = regular key
    // this order should not change LUA script depends on it
    /// <summary>
    /// Script that HGET's the key's value only if it exists in the evicted store
    /// </summary>
    internal const string GetAndRefreshEvictedScript = (@"
                  local sub = function (key)
                  local bulk = redis.call('HGETALL', key)
	                 local result = {}
	                 local nextkey
	                 for i, v in ipairs(bulk) do
		                 if i % 2 == 1 then
			                 nextkey = v
		                 else
			                 result[nextkey] = v
		                 end
	                 end
	                 return result
                end

                local result = sub(KEYS[1])

                if next(result) == nil then
                  local regular_result = redis.call('EXISTS', ARGV[2])
                  if regular_result ~= 0 then
                     return '0'
                  end
                  return nil
                end

                local sldexp = tonumber(result['sldexp'])
                local absexp = tonumber(result['absexp'])

                if sldexp == -1 then
                  if ARGV[1] == '1' then
                    return result['data']
                  else
                    return '1'
                  end
                end

                local exp = 1
                local time = tonumber(redis.call('TIME')[1])
                if absexp ~= -1 then
                  local relexp = absexp - time
                  if relexp <= sldexp then
                    exp = relexp
                  else
                    exp = sldexp                   
                  end
                else
                  exp = sldexp
                end
                
                redis.call('EXPIRE', KEYS[1], exp, 'XX')
                                
                if ARGV[1] == '1' then
                  return result['data']
                end
                return '1'");
    
    internal const string AbsoluteExpirationKey = "absexp";
    internal const string SlidingExpirationKey = "sldexp";
    internal const string DataKey = "data";
    internal const string SuccessfulScriptNoDataReturnedValue = "1";
    internal const string UnsuccessfulScriptOtherCacheHasKeyReturnedValue = "0";
    internal const string NoKeyFoundInAnyCacheReturnValue = "nil";
    internal const string ReturnDataArg = "1";
    internal const string DontReturnDataArg = "0";
    internal const long NotPresent = -1;
}
