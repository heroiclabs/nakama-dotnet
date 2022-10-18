// Copyright 2022 The Satori Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

namespace Satori
{
    /// <summary>
    /// A delegate used to determine whether or not a network exception is
    /// due to a temporary bad state on the server. For example, timeouts can be transient in cases where
    /// the server is experiencing temporarily high load.
    /// </summary>
    public delegate bool TransientExceptionDelegate(Exception e);
}
