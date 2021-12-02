﻿using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace NetPro.CsRedis
{
    /// <summary>
    /// Redis配置
    /// </summary>
    public class RedisCacheOption
    {
        /// <summary>
        /// 
        /// </summary>
        public RedisCacheOption()
        {

        }

        /// <summary>
        /// root node is Redis
        /// </summary>
        /// <param name="config"></param>
        public RedisCacheOption(IConfiguration config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            config.GetSection(nameof(RedisCacheOption)).Bind(this);

        }

        /// <summary>
        /// 连接串集合
        /// </summary>
        public List<ConnectionString> ConnectionString { get; set; }

        /// <summary>
        ///是否启用
        ///true:启用，false:禁用
        /// </summary>
        public bool Enabled { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class ConnectionString
    {
        /// <summary>
        /// 连接串别名
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 连接串
        /// </summary>
        public string Value { get; set; }
    }
}
