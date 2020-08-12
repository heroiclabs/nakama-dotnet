/*
 * Copyright 2020 Heroic Labs
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


/// <summary>
/// A custom attribute recognized by Unity3D. When added to a class member, it prevents
/// the Unity linker from stripping the code it is associated with. This is used in addition
/// to the link.xml file because the Unity Package Manager does not recognize link.xml files
/// inside Unity packages.
/// https://docs.unity3d.com/2018.3/Documentation/Manual/ManagedCodeStripping.html
/// </summary>
internal class PreserveAttribute : System.Attribute {}
