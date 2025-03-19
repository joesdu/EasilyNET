[^ **\_\_**Back**\_\_** ^](..\README.md)

### 当前互联网地图的坐标系现状

#### 地球坐标(WGS84)

- 国际标准,从 GPS 设备中取出的数据的坐标系
- 国际地图提供商使用的坐标系

#### 火星坐标(GCJ-02)也叫国测局坐标系

- 中国标准,从国行移动设备中定位获取的坐标数据使用这个坐标系
- 国家规定: 国内出版的各种地图系统(包括电子形式),必须至少采用 GCJ-02 对地理位置进行首次加密.

#### 百度坐标(BD-09)

- 百度标准,百度 SDK,百度地图,GeoCoding 使用 -(本来就乱了,百度又在火星坐标上来个二次加密)

### 开发过程需要注意的事

- 从设备获取经纬度(GPS)坐标

  如果使用的是百度 sdk 那么可以获得百度坐标(bd09)或者火星坐标(GCJ02),默认是 bd09 如果使用的是 ios 的原生定位库,那么获得的坐标是
  WGS84 如果使用的是高德 sdk,那么获取的坐标是 GCJ02

- 互联网在线地图使用的坐标系

      火星坐标系:

  iOS 地图(其实是高德) Google 国内地图(.cn 域名下) 搜搜、阿里云、高德地图、腾讯百度坐标系: 当然只有百度地图 WGS84 坐标系:
  国际标准,谷歌国外地图、osm 地图等国外的地图一般都是这个

<details>
<summary style="font-size: 14px">English</summary>

### Current status of coordinate systems on the Internet map

#### Earth coordinates (WGS84)

- International standard, the coordinate system of data taken from GPS devices
- Coordinate system used by international map providers

#### Mars coordinates (GCJ-02) also known as the national coordinate system

- Chinese standard, the coordinate data obtained from domestic mobile devices is used in this coordinate system
- National regulations: Various map systems published in China (including electronic form) must encrypt geographic
  locations at least once using GCJ-02.

#### Baidu coordinates (BD-09)

- Baidu standard, Baidu SDK, Baidu Maps, GeoCoding use -(originally confused, Baidu adds a second encryption to Mars
  coordinates)

#### Things to note in the development process

- Get latitude and longitude (GPS) coordinates from the device
  If you are using the Baidu SDK, you can get Baidu coordinates (bd09) or Mars coordinates (GCJ02), which are bd09 by
  default. If you are using the native iOS positioning library, the coordinates you get are WGS84. If you are using the
  Gaode SDK, the coordinates you get are GCJ02.

- Coordinate system used by Internet online maps

       Mars coordinate system:

  iOS Maps (actually Gaode) Google domestic maps (.cn domain) Soso, Aliyun, Gaode Maps, Tencent Baidu coordinate system:
  Of course, only Baidu Maps WGS84 coordinate system: International standard, Google foreign maps, osm maps, etc.,
  foreign maps are generally this

</details>

```csharp

CoordinateConvert.BD09ToGCJ02(116.404, 39.915, out var gcjLon, out var gcjLat);
CoordinateConvert.GCJ02ToBD09(116.404, 39.915, out var bdLon, out var bdLat);
CoordinateConvert.WGS84ToGCJ02(116.404, 39.915, out var gcjLon2, out var gcjLat2);
CoordinateConvert.GCJ02ToWGS84(116.404, 39.915, out var wgsLon, out var wgsLat);

Console.WriteLine($"百度经纬度坐标转国测局坐标,经度:{gcjLon},纬度:{gcjLat}");
Console.WriteLine($"国测局坐标转百度经纬度坐标,经度:{bdLon},纬度:{bdLat}");
Console.WriteLine($"WGS84转国测局坐标,经度:{gcjLon2},纬度:{gcjLat2}");
Console.WriteLine($"国测局坐标转WGS84坐标,经度:{wgsLon},纬度:{wgsLat}");

// Output:

// 百度经纬度坐标转国测局坐标,经度:116.39762729119315,纬度:39.90865673957631
// 国测局坐标转百度经纬度坐标,经度:116.41036949371029,纬度:39.92133699351021
// WGS84转国测局坐标,经度:116.41024449916938,纬度:39.91601738120746
// 国测局坐标转WGS84坐标,经度:116.39775550083061,纬度:39.91398261879254
```
