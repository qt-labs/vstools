/***************************************************************************************************
 Copyright (C) 2024 The Qt Company Ltd.
 SPDX-License-Identifier: LicenseRef-Qt-Commercial OR LGPL-3.0-only OR GPL-2.0-only OR GPL-3.0-only
***************************************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace QtVsTools.VisualStudio
{
    using ServiceType = Tuple<Type, Type>;

    public interface IVsServiceProvider
    {
        I GetService<T, I>() where T : class where I : class;
        Task<I> GetServiceAsync<T, I>() where T : class where I : class;
    }

    public static class VsServiceProvider
    {
        public static IVsServiceProvider Instance { get; set; }

        static readonly ConcurrentDictionary<ServiceType, object> services = new();

        public static I GetService<I>()
            where I : class
        {
            return GetService<I, I>();
        }

        public static I GetService<T, I>()
            where T : class
            where I : class
        {
            if (Instance == null)
                return null;

            if (services.TryGetValue(new ServiceType(typeof(T), typeof(I)), out object serviceObj))
                return serviceObj as I;

            var serviceInterface = Instance.GetService<T, I>();
            services.TryAdd(new ServiceType(typeof(T), typeof(I)), serviceInterface);
            return serviceInterface;
        }

        public static async Task<I> GetServiceAsync<I>()
            where I : class
        {
            return await GetServiceAsync<I, I>();
        }

        public static async Task<I> GetServiceAsync<T, I>()
            where T : class
            where I : class
        {
            if (Instance == null)
                return null;

            if (services.TryGetValue(new ServiceType(typeof(T), typeof(I)), out object serviceObj))
                return serviceObj as I;

            var serviceInterface = await Instance.GetServiceAsync<T, I>();
            services.TryAdd(new ServiceType(typeof(T), typeof(I)), serviceInterface);
            return serviceInterface;
        }

        public static I GetGlobalService<T, I>()
            where T : class
            where I : class
        {
            return Package.GetGlobalService(typeof(T)) as I;
        }
    }
}
