﻿using CSRedis;
using Microsoft.Extensions.Logging;
using NetPro.CsRedis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CSRedis.CSRedisClient;

namespace XXX.Plugin.Web.Demo
{
    public interface IRedisService
    {
        int DistributeLock(string lockKey, int timeoutSeconds = 30, bool autoDelay = false);
        Task<string> GetAsync(string key = "one_key");
        Task<long> PublishAsync(string channel, string message);
        Task<bool> SetAsync(string key = "one_key", string value = "one_value", TimeSpan? timeSpan = null);
    }

    public class RedisService : IRedisService
    {
        private readonly ILogger<RedisService> _logger;
        private readonly IRedisManager _redisManager;//NetPro封装过的对象接口，提供更丰富功能，本地缓存等
        private readonly IdleBus<CSRedisClient> _redisClient;//CSRedis原生对象，以最原生方式调用，支持操作多个redis库；_redisClient.Get("别名")必须先取出别名对应的redis实例对象进行奥操作

        private static int tempInt = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="redisManager"></param>
        /// <param name="redisClient"></param>
        public RedisService(ILogger<RedisService> logger
            , IRedisManager redisManager
            , IdleBus<CSRedisClient> redisClient)
        {
            _logger = logger;
            _redisManager = redisManager;
            _redisClient = redisClient;
        }

        /// <summary>
        /// Redis插入
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public async Task<bool> SetAsync(string key = "one_key", string value = "one_value", TimeSpan? timeSpan = null)
        {
            //remark:
            //一秒后过期
            TimeSpan.FromSeconds(1);
            //一小时后过期
            TimeSpan.FromHours(1);

            //获取依赖注入的对象方式一：通过静态EngineContext对象引擎获取对象实例
            //var redisManager = EngineContext.Current.Resolve<IRedisManager>();

            //获取依赖注入的对象方式二：通过构造函数注入获取对象实例
            var succeed = await _redisManager.SetAsync(key, value, timeSpan);
            _logger.LogError("redis 插入成功 ");
            return succeed;
        }

        /// <summary>
        /// 通过key或者Redis值
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<string> GetAsync(string key = "one_key")
        {
            //可随时改用原生对象操作redis
            var value = await _redisManager.GetAsync<string>(key);
            _logger.LogError("redis 查询成功 ");
            return value;
        }

        /// <summary>
        /// 消息发布
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<long> PublishAsync(string channel, string message)
        {
            //发布到别名为1的redis数据库中
            var value = await _redisManager.PublishAsync(channel, message);
            _logger.LogInformation("redis 查询成功 ");
            return value;
        }

        /// <summary>
        /// 分布式锁
        /// </summary>
        /// <param name="lockKey">指定锁的key</param>
        /// <param name="timeoutSeconds">超时秒数，超过时间自动释放锁</param>
        /// <param name="autoDelay">是否需要自动延时,true:自动延长过期时间false:不延长过期时间，以设置的过期时间为准</param>
        /// <returns></returns>
        public int DistributeLock(string lockKey, int timeoutSeconds = 30, bool autoDelay = false)
        {
            //通过别名为1的redis库进行分布式锁
            using (_redisClient.Get("1").Lock(lockKey, timeoutSeconds, autoDelay))
            {
                //被锁住的逻辑
                _logger.LogInformation($"分布式锁的当前值---{tempInt++}");
                return tempInt;
            }
        }
    }

    /// <summary>
    /// 启动任务
    /// 继承IStartupTask的对象启动执行一次，一般可将后台任务初始化等放于此
    /// </summary>
    class RedisTask : IStartupTask
    {
        private readonly IdleBus<CSRedisClient> _redisClient;
        private readonly IRedisManager _redisManager;
        private readonly ILogger<RedisTask> _logger;

        /// <summary>
        /// ctor
        /// </summary>
        public RedisTask()
        {
            _redisClient = EngineContext.Current.Resolve<IdleBus<CSRedisClient>>();//获取支持多个库的原生Redisclient对象
            _redisManager= EngineContext.Current.Resolve<IRedisManager>();//获取封装过的redis库的IRedisManager对象
            _logger = EngineContext.Current.Resolve<ILogger<RedisTask>>();
        }
        int IStartupTask.Order => 0;

        /// <summary>
        /// 订阅消费
        /// redis发布订阅几种方式 reference：https://www.cnblogs.com/kellynic/p/9952386.html
        /// github issue reference ：https://github.com/2881099/csredis/issues/202
        /// </summary>
        /// <returns></returns>
        void IStartupTask.Execute()
        {
            _redisClient.Get("1").Subscribe
               (
               ("runoobChat", msg =>
               {
                   //消费第1个订阅
                   _logger.LogWarning($"消费中--message={msg.Body}");
               }
            ));

            _redisClient.Get("1").Subscribe
               (
               ("runoobChat2", msg =>
               {
                   //消费第2个订阅
                   _logger.LogWarning($"消费中--message={msg.Body}");
               }
            ));
        }
    }

}