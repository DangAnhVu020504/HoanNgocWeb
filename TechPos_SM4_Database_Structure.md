# Cau truc co so du lieu - TechPos SM4 v4.5

**He quan tri:** Firebird (Embedded)  |  **File du lieu:** Database/DEMO.FDB, Data/TEMPLATE.FDB (duoi .FDB, ~19MB)

Tong so bang/view: **86**. Hai file DEMO.FDB va TEMPLATE.FDB co cau truc giong het nhau.

> Moi bang deu co cac cot he thong chuan: ID VARCHAR(36) (khoa chinh GUID), STATUS, USERCREATEDID, TIMECREATED, USERMODIFIEDID, TIMEMODIFIED.

## Muc luc

- **Kho va Ton kho**
  - [OTONKHO](#otonkho)
  - [TLUUTAM](#tluutam)
  - [TLUUTAMCHITIET](#tluutamchitiet)
  - [TKHOASO](#tkhoaso)
- **Dich vu Cham soc va Hop dong**
  - [TCHAMSOC](#tchamsoc)
  - [THOPDONG](#thopdong)
  - [THOPDONGCHITIET](#thopdongchitiet)
  - [TGIAOHANGTHEOXE](#tgiaohangtheoxe)
  - [TGIAOHANGTHEOXECHITIET](#tgiaohangtheoxechitiet)
  - [TGUIHANGCOD](#tguihangcod)
  - [TKETQUAGUICOD](#tketquaguicod)
  - [TGIAYGIOITHIEU](#tgiaygioithieu)
- **Nhan su va Luong**
  - [TBANGLUONG](#tbangluong)
  - [TBANGLUONGCHITIET](#tbangluongchitiet)
  - [TBANGLUONGTONGHOP](#tbangluongtonghop)
  - [TTHUONGPHAT](#tthuongphat)
  - [TLUUVET](#tluuvet)
- **He thong va Cau hinh (System)**
  - [SUSER](#suser)
  - [SGROUPUSER](#sgroupuser)
  - [SGROUPROLE](#sgrouprole)
  - [SMENU](#smenu)
  - [SFUNCTION](#sfunction)
  - [SCONFIG](#sconfig)
  - [SCONFIGGROUP](#sconfiggroup)
  - [STABLEDESC](#stabledesc)
  - [SCOLUMN](#scolumn)
  - [SGRID](#sgrid)
  - [SFORM](#sform)
  - [SREPORT](#sreport)
  - [SREPORTROLE](#sreportrole)
  - [SREPORTTEMPLATE](#sreporttemplate)
  - [SIMAGE](#simage)
  - [STEMPLATE](#stemplate)
  - [SANALYSIS](#sanalysis)
  - [STRACKING](#stracking)
  - [SQUICKNOTE](#squicknote)
  - [SSMSTEMPLATE](#ssmstemplate)
  - [TNGUOIDUNGTHEOCUAHANG](#tnguoidungtheocuahang)
- **Danh muc (Dictionary)**
  - [DCALAMVIEC](#dcalamviec)
  - [DCHITIEUCHITIET](#dchitieuchitiet)
  - [DCHITIEUDOANHTHU](#dchitieudoanhthu)
  - [DCUAHANG](#dcuahang)
  - [DDINHLUONG](#ddinhluong)
  - [DDOITACKYGUI](#ddoitackygui)
  - [DDONVICHUYENPHAT](#ddonvichuyenphat)
  - [DDONVITINH](#ddonvitinh)
  - [DDOTKHUYENMAI](#ddotkhuyenmai)
  - [DDOTKHUYENMAICHITIET](#ddotkhuyenmaichitiet)
  - [DHANGSANXUAT](#dhangsanxuat)
  - [DKHACHHANG](#dkhachhang)
  - [DKHOHANG](#dkhohang)
  - [DLOAIHINHKHUYENMAI](#dloaihinhkhuyenmai)
  - [DLOAIHOPDONG](#dloaihopdong)
  - [DLOAIQUY](#dloaiquy)
  - [DLYDOTHUCHI](#dlydothuchi)
  - [DLYDOTHUONGPHAT](#dlydothuongphat)
  - [DMATHANG](#dmathang)
  - [DNHACUNGCAP](#dnhacungcap)
  - [DNHANVIEN](#dnhanvien)
  - [DNHOMCHITIEUDOANHTHU](#dnhomchitieudoanhthu)
  - [DNHOMKHACHHANG](#dnhomkhachhang)
  - [DNHOMMATHANG](#dnhommathang)
  - [DNHOMNHACUNGCAP](#dnhomnhacungcap)
  - [DNHOMYKIENKHACHHANG](#dnhomykienkhachhang)
  - [DTAIKHOANNGANHANG](#dtaikhoannganhang)
  - [DTINHTHANH](#dtinhthanh)
  - [DTINHTRANGGOI](#dtinhtranggoi)
  - [DTRANGTHAIHOPDONG](#dtrangthaihopdong)
  - [DXEGIAOHANG](#dxegiaohang)
  - [DYKIENKHACHHANG](#dykienkhachhang)
- **Nghiep vu Ban hang va Dat hang**
  - [TDONHANG](#tdonhang)
  - [TDONHANGCHITIET](#tdonhangchitiet)
  - [TDONHANGHUY](#tdonhanghuy)
  - [TDONHANGHUYCHITIET](#tdonhanghuychitiet)
  - [TDATHANG](#tdathang)
  - [TDATHANGCHITIET](#tdathangchitiet)
  - [TDATHANGNCC](#tdathangncc)
  - [TDATHANGNCCCHITIET](#tdathangnccchitiet)
  - [TCHITIETTHANHTOAN](#tchitietthanhtoan)
  - [TTHUCHI](#tthuchi)
  - [TTHUCHIHANGLOAT](#tthuchihangloat)
  - [TDOINO](#tdoino)
  - [TBAOGIA](#tbaogia)
  - [TBAOGIACHITIET](#tbaogiachitiet)
  - [TGIAMTHEONHOM](#tgiamtheonhom)
  - [TTANGGIAMDIEM](#ttanggiamdiem)

---

## Kho va Ton kho

### <a name="otonkho"></a>OTONKHO

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DKHOHANGID` | VARCHAR(36) |  |
| `DMATHANGID` | VARCHAR(36) |  |
| `TON` | BIGINT |  |

### <a name="tluutam"></a>TLUUTAM

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `NGAY` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DOITRA` | BIGINT |  |
| `GIAOHANG` | VARCHAR(255) |  |
| `TDATHANGID` | VARCHAR(36) |  |
| `LOAIGIA` | INTEGER |  |
| `DIENGIAI` | VARCHAR(255) |  |
| `DNHANVIENXUATID` | VARCHAR(36) |  |
| `DKHOXUATID` | VARCHAR(36) |  |
| `TIENHANG` | BIGINT |  |
| `TILETHUE` | BIGINT |  |
| `TIENTHUE` | BIGINT |  |
| `TILEGIAMGIA` | BIGINT |  |
| `TIENGIAMGIA` | BIGINT |  |
| `PHIVANCHUYEN` | BIGINT |  |
| `TONGCONG` | BIGINT |  |
| `GIAMTHEOTIEN` | INTEGER |  |
| `DKHACHHANGID` | VARCHAR(36) |  |

### <a name="tluutamchitiet"></a>TLUUTAMCHITIET

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `TLUUTAMID` | VARCHAR(36) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `SOLUONG` | BIGINT |  |
| `DONGIA` | BIGINT |  |
| `THANHTIEN` | BIGINT |  |
| `TILEGIAMGIA` | BIGINT |  |
| `BAOHANH` | VARCHAR(255) |  |
| `DMATHANGID` | VARCHAR(36) |  |
| `SOLUONGCHUAQUYDOI` | BIGINT |  |
| `DDONVITINHID` | VARCHAR(36) |  |

### <a name="tkhoaso"></a>TKHOASO

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `NGAY` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |

## Dich vu Cham soc va Hop dong

### <a name="tchamsoc"></a>TCHAMSOC

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `NGAY` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DNHANVIENID` | VARCHAR(36) |  |
| `GIO` | TIMESTAMP |  |
| `DTINHTRANGGOIID` | VARCHAR(36) |  |
| `DKHACHHANGID` | VARCHAR(36) |  |

### <a name="thopdong"></a>THOPDONG

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `NGAY` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `NGAYKY` | TIMESTAMP |  |
| `TUNGAY` | TIMESTAMP |  |
| `DENNGAY` | TIMESTAMP |  |
| `NGAYNGHIEMTHU` | TIMESTAMP |  |
| `NGAYTHANHLY` | TIMESTAMP |  |
| `DNHANVIENID` | VARCHAR(36) |  |
| `DKHACHHANGID` | VARCHAR(36) |  |
| `DLOAIHOPDONGID` | VARCHAR(36) |  |
| `TONGCONG` | BIGINT |  |
| `DATCOC` | BIGINT |  |
| `DTRANGTHAIHOPDONGID` | VARCHAR(36) |  |

### <a name="thopdongchitiet"></a>THOPDONGCHITIET

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `THOPDONGID` | VARCHAR(36) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DMATHANGID` | VARCHAR(36) |  |
| `SOLUONG` | BIGINT |  |
| `DONGIA` | BIGINT |  |
| `THANHTIEN` | BIGINT |  |
| `GIAMGIA` | BIGINT |  |
| `TILEGIAMGIA` | BIGINT |  |

### <a name="tgiaohangtheoxe"></a>TGIAOHANGTHEOXE

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `NGAY` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DNHANVIENID` | VARCHAR(36) |  |
| `DXEGIAOHANGID` | VARCHAR(36) |  |

### <a name="tgiaohangtheoxechitiet"></a>TGIAOHANGTHEOXECHITIET

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `TGIAOHANGTHEOXEID` | VARCHAR(36) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `TDONHANGID` | VARCHAR(36) |  |

### <a name="tguihangcod"></a>TGUIHANGCOD

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `NGAY` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DDONVICHUYENPHATID` | VARCHAR(36) |  |
| `DNHANVIENID` | VARCHAR(36) |  |

### <a name="tketquaguicod"></a>TKETQUAGUICOD

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `NGAY` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DNHANVIENID` | VARCHAR(36) |  |
| `DDONVICHUYENPHATID` | VARCHAR(36) |  |

### <a name="tgiaygioithieu"></a>TGIAYGIOITHIEU

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `NGAY` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `HIEULUCDEN` | TIMESTAMP |  |
| `KINHGUI` | VARCHAR(255) |  |
| `DIACHI` | VARCHAR(255) |  |
| `NGUOILIENHE` | VARCHAR(255) |  |
| `ONGBA` | VARCHAR(255) |  |
| `DUOCCUDEN` | VARCHAR(255) |  |
| `VEVIEC` | VARCHAR(255) |  |

## Nhan su va Luong

### <a name="tbangluong"></a>TBANGLUONG

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `NGAY` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `THANG` | INTEGER |  |
| `NAM` | INTEGER |  |
| `CHITIET` | BLOB |  |

### <a name="tbangluongchitiet"></a>TBANGLUONGCHITIET

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `TBANGLUONGID` | VARCHAR(36) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DNHANVIENID` | VARCHAR(36) |  |
| `DCALAMVIECID` | VARCHAR(36) |  |
| `TRANGTHAI` | INTEGER |  |
| `NGAY` | TIMESTAMP |  |

### <a name="tbangluongtonghop"></a>TBANGLUONGTONGHOP

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DNHANVIENID` | VARCHAR(36) |  |
| `LUONGCA` | BIGINT |  |
| `TONGLUONG` | BIGINT |  |
| `PHAT` | BIGINT |  |
| `TAMUNG` | BIGINT |  |
| `THUCNHAN` | BIGINT |  |
| `THUONG` | BIGINT |  |
| `LUONGTHANG` | BIGINT |  |
| `CACHTINHLUONG` | INTEGER |  |
| `TBANGLUONGID` | VARCHAR(36) |  |
| `PHUCAP` | BIGINT |  |

### <a name="tthuongphat"></a>TTHUONGPHAT

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `NGAY` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DNHANVIENID` | VARCHAR(36) |  |
| `THUONG` | BIGINT |  |
| `PHAT` | BIGINT |  |
| `DLYDOTHUONGPHATID` | VARCHAR(36) |  |

### <a name="tluuvet"></a>TLUUVET

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `NGAY` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `GIO` | TIMESTAMP |  |
| `SODONHANG` | VARCHAR(36) |  |
| `TAIKHOAN` | VARCHAR(36) |  |

## He thong va Cau hinh (System)

### <a name="suser"></a>SUSER

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `PASSWORD` | VARCHAR(255) |  |
| `USERNAME` | VARCHAR(255) |  |
| `EMAIL` | VARCHAR(255) |  |
| `ISADMIN` | INTEGER |  |
| `SGROUPUSERID` | VARCHAR(36) |  |
| `SMTP` | VARCHAR(255) |  |
| `SSL` | INTEGER |  |
| `PORT` | INTEGER |  |
| `PASS` | VARCHAR(255) |  |
| `VIETINPUT` | INTEGER |  |
| `INPUTMODE` | INTEGER |  |
| `SIMAGEID` | VARCHAR(36) |  |
| `AUTOID` | BIGINT |  |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `SORTORDER` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `DNHANVIENID` | VARCHAR(36) |  |
| `DEFAULTFUNCID` | VARCHAR(36) |  |
| `DKHOHANGID` | VARCHAR(36) |  |
| `USERID` | VARCHAR(36) |  |
| `CARDCODE` | VARCHAR(255) |  |

### <a name="sgroupuser"></a>SGROUPUSER

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `SIMAGEID` | VARCHAR(36) |  |
| `ITEMTYPE` | INTEGER |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `PARENTID` | VARCHAR(36) |  |
| `SORTORDER` | VARCHAR(255) |  |
| `AUTOID` | BIGINT |  |

### <a name="sgrouprole"></a>SGROUPROLE

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `SGROUPUSERID` | VARCHAR(36) |  |
| `SFUNCTIONID` | VARCHAR(36) |  |
| `MODE` | INTEGER |  |

### <a name="smenu"></a>SMENU

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | VARCHAR(255) |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |
| `OPERATION` | INTEGER |  |
| `STABLEDESCID` | VARCHAR(36) |  |
| `SFORMID` | VARCHAR(36) |  |
| `BOLD` | INTEGER |  |
| `ITALIC` | INTEGER |  |
| `IMAGE` | BLOB |  |
| `TOOLBAR` | INTEGER |  |
| `TOOLBARINDEX` | INTEGER |  |
| `SHORTCUT` | INTEGER |  |
| `TOOLBARTITLTE` | VARCHAR(255) |  |
| `VISIBLECONFIGID` | VARCHAR(36) |  |
| `SREPORTID` | VARCHAR(36) |  |
| `VIEWCOUNT` | INTEGER |  |
| `SFUNCTIONID` | VARCHAR(36) |  |
| `LOAI` | INTEGER |  |
| `LOCTHEOLOAI` | INTEGER |  |
| `HIDEINMENU` | INTEGER |  |
| `MENUCODE` | VARCHAR(255) |  |
| `VIEWONTS` | INTEGER |  |
| `VIEWONMOBI` | INTEGER |  |
| `VIEWONWEB` | INTEGER |  |

### <a name="sfunction"></a>SFUNCTION

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | INTEGER |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `GROUPNAME` | VARCHAR(255) |  |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |

### <a name="sconfig"></a>SCONFIG

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | INTEGER |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `TEXTVALUE` | VARCHAR(255) |  |
| `DATETIMEVALUE` | TIMESTAMP |  |
| `INTVALUE` | INTEGER |  |
| `DECIMALVALUE` | BIGINT |  |
| `DATATYPE` | INTEGER |  |
| `BLOBVALUE` | BLOB |  |
| `CAPTION` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `SIMAGEID` | VARCHAR(36) |  |
| `MOREDETAIL` | VARCHAR(255) |  |
| `CONTROLTYPE` | INTEGER |  |
| `REFTABLEID` | VARCHAR(36) |  |
| `OTHERCONFIG` | VARCHAR(255) |  |
| `SCONFIGGROUPID` | VARCHAR(36) |  |
| `TAB` | INTEGER |  |
| `SOCOT` | INTEGER |  |
| `HEADER` | VARCHAR(255) |  |
| `FOOTER` | VARCHAR(255) |  |
| `SHOWONREPORT` | INTEGER |  |
| `PARENTID` | VARCHAR(36) |  |

### <a name="sconfiggroup"></a>SCONFIGGROUP

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | VARCHAR(255) |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |
| `IMAGE` | BLOB |  |
| `MAUHOADON` | INTEGER |  |

### <a name="stabledesc"></a>STABLEDESC

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DEFAULTID` | VARCHAR(36) |  |
| `SORTBY` | INTEGER |  |
| `DESCRIPTION` | VARCHAR(255) |  |
| `TABLETYPE` | INTEGER |  |
| `COTTANG` | VARCHAR(36) |  |
| `COTGIAM` | VARCHAR(36) |  |
| `SFUNCTIONID` | VARCHAR(36) |  |
| `AELAYOUT` | BLOB |  |
| `SORTORDER` | VARCHAR(255) |  |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |
| `ISSYSTEM` | INTEGER |  |
| `USEDEFAULTAEFORM` | INTEGER |  |
| `GENNO` | INTEGER |  |
| `NOTEMPLATE` | VARCHAR(36) |  |
| `IMAGE32` | BLOB |  |
| `IMAGE` | BLOB |  |
| `CODE` | BLOB |  |
| `DESIGNCODE` | BLOB |  |
| `CATEGORYCOL` | VARCHAR(36) |  |
| `FILTERCONFIG` | BLOB |  |
| `INVOICEPARAM` | BLOB |  |
| `INVOICEDETAILPARAM` | BLOB |  |
| `INVOICEPRINT` | INTEGER |  |
| `BARCODEPRINT` | INTEGER |  |
| `LASTTEMPLATEID` | VARCHAR(36) |  |
| `MUSTSELECTCATEGORY` | INTEGER |  |
| `MASTERTABLEID` | VARCHAR(36) |  |
| `FORMAT` | BLOB |  |
| `REFCONFIG` | BLOB |  |
| `AUTOREPORT` | INTEGER |  |
| `SHOWTOTAL` | INTEGER |  |
| `READONLY` | INTEGER |  |
| `CLIENTCODE` | BLOB |  |
| `SERVERCODE` | BLOB |  |
| `SYNCOPTION` | VARCHAR(400) |  |
| `TSCODE` | BLOB |  |
| `TSLAYOUT` | BLOB |  |
| `TSDESIGNCODE` | BLOB |  |

### <a name="scolumn"></a>SCOLUMN

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `STABLEDESCID` | VARCHAR(36) |  |
| `CAPTION` | VARCHAR(255) |  |
| `FORMAT` | VARCHAR(36) |  |
| `REFTABLEID` | VARCHAR(36) |  |
| `ALLOWEMPTY` | VARCHAR(36) |  |
| `ALLOWDUPLICATE` | VARCHAR(36) |  |
| `ISSYSTEM` | INTEGER |  |
| `ENABLECONFIGID` | VARCHAR(36) |  |
| `TITLECONFIGID` | VARCHAR(36) |  |
| `TOOLTIP` | VARCHAR(255) |  |
| `SFUNCTIONID` | VARCHAR(36) |  |
| `MAXVALUE` | BIGINT |  |
| `MINVALUE` | BIGINT |  |
| `DEFAULTCONFIGID` | VARCHAR(36) |  |
| `TAG` | VARCHAR(255) |  |
| `CONTROLTYPE` | VARCHAR(36) |  |
| `SHOWONLOOKUP` | INTEGER |  |
| `FORMULA` | VARCHAR(255) |  |
| `AUTOREPORT` | VARCHAR(255) |  |

### <a name="sgrid"></a>SGRID

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(1000) | NOT NULL |
| `NOTE` | VARCHAR(1) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `MOBILE` | VARCHAR(255) |  |
| `GRIDID` | VARCHAR(36) |  |

### <a name="sform"></a>SFORM

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `SORTORDER` | VARCHAR(255) |  |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |
| `CODE` | BLOB |  |
| `DESIGNCODE` | BLOB |  |
| `FORMTYPE` | INTEGER |  |
| `STABLEDESCID` | VARCHAR(36) |  |
| `NOTEMPLATE` | VARCHAR(36) |  |
| `AELAYOUT` | BLOB |  |
| `LOAI` | INTEGER |  |
| `IMAGE32` | BLOB |  |
| `TABLEDESC` | VARCHAR(255) |  |
| `LASTTEMPLATEID` | VARCHAR(36) |  |
| `CLASSNAME` | VARCHAR(36) |  |
| `FILTERCONFIG` | BLOB |  |
| `REFCONFIG` | BLOB |  |
| `REPORTDESC` | VARCHAR(255) |  |
| `SFUNCTIONID` | VARCHAR(36) |  |
| `SHOWTOTAL` | INTEGER |  |
| `CLIENTCODE` | BLOB |  |
| `SERVERCODE` | BLOB |  |
| `TSCODE` | BLOB |  |
| `TSLAYOUT` | BLOB |  |
| `TSDESIGNCODE` | BLOB |  |

### <a name="sreport"></a>SREPORT

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | VARCHAR(255) |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |
| `IMAGE32` | BLOB |  |
| `FILTERCONFIG` | BLOB |  |
| `VERTICALLAYOUT` | INTEGER |  |
| `RUNCOUNT` | INTEGER |  |
| `MASTERVIEW` | BLOB |  |
| `DETAILVIEW` | BLOB |  |
| `HASDETAIL` | INTEGER |  |
| `FILTERONTOP` | INTEGER |  |
| `HASDETAIL2` | INTEGER |  |
| `DETAIL2VIEW` | BLOB |  |
| `VIEWINREPORTMG` | INTEGER |  |
| `SQL` | BLOB |  |
| `PARAMS` | BLOB |  |
| `LASTTEMPLATEID` | VARCHAR(36) |  |
| `SQLMODE` | INTEGER |  |
| `REMINDER` | INTEGER |  |
| `FORMAT` | BLOB |  |
| `THUONGDUNG` | INTEGER |  |
| `CODE` | BLOB |  |
| `COLCONFIG` | BLOB |  |
| `RAWMODE` | INTEGER |  |
| `CLASSNAME` | VARCHAR(255) |  |
| `SERVERCODE` | BLOB |  |
| `CLIENTCODE` | BLOB |  |

### <a name="sreportrole"></a>SREPORTROLE

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `STATUS` | INTEGER |  |
| `MODE` | INTEGER |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `ID` | VARCHAR(36) | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `USERCREATEDID` | VARCHAR(36) |  |
| `SGROUPUSERID` | VARCHAR(36) |  |
| `SREPORTID` | VARCHAR(36) |  |

### <a name="sreporttemplate"></a>SREPORTTEMPLATE

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `TEMPLATE` | BLOB |  |
| `STEMPLATEID` | VARCHAR(36) |  |
| `SREPORTID` | VARCHAR(36) |  |
| `CONFIG` | BLOB |  |
| `SORTORDER` | VARCHAR(255) |  |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |
| `AUTOGENREPORT` | INTEGER |  |
| `COLAUTOWIDTH` | INTEGER |  |
| `STEMPLATELANDSCAPEID` | VARCHAR(36) |  |

### <a name="simage"></a>SIMAGE

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | INTEGER |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `IMAGE` | BLOB |  |

### <a name="stemplate"></a>STEMPLATE

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | VARCHAR(255) |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |
| `TEMPLATE` | BLOB |  |
| `STABLEDESCID` | VARCHAR(36) |  |
| `REPORTBASE` | INTEGER |  |
| `SFORMID` | VARCHAR(36) |  |
| `BARCODE` | INTEGER |  |
| `BARCODECOL` | INTEGER |  |
| `STYLE` | BLOB |  |

### <a name="sanalysis"></a>SANALYSIS

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | VARCHAR(255) |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |
| `CONFIG` | BLOB |  |
| `ANALYSISTYPE` | INTEGER |  |
| `COTTONG` | VARCHAR(255) |  |

### <a name="stracking"></a>STRACKING

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `STATUS` | INTEGER |  |
| `LOAI` | INTEGER |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `NGAY` | TIMESTAMP |  |
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) |  |
| `NOTE` | VARCHAR(255) |  |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `USERCREATEDID` | VARCHAR(36) |  |
| `TAIKHOAN` | VARCHAR(255) |  |
| `IP` | VARCHAR(255) |  |
| `DOITUONG` | VARCHAR(255) |  |
| `GIO` | VARCHAR(255) |  |
| `REFID` | VARCHAR(36) |  |
| `STABLEDESCID` | VARCHAR(36) |  |
| `STRACKINGTYPEID` | VARCHAR(36) |  |

### <a name="squicknote"></a>SQUICKNOTE

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | INTEGER |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `NOTEDATA` | BLOB |  |

### <a name="ssmstemplate"></a>SSMSTEMPLATE

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `AUTOID` | BIGINT |  |
| `STATUS` | INTEGER |  |
| `ITEMTYPE` | INTEGER |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) |  |
| `NOTE` | VARCHAR(255) |  |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `SORTORDER` | VARCHAR(255) |  |
| `USERCREATEDID` | VARCHAR(36) |  |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `SIMAGEID` | VARCHAR(36) |  |

### <a name="tnguoidungtheocuahang"></a>TNGUOIDUNGTHEOCUAHANG

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `SUSERID` | VARCHAR(36) |  |
| `DCUAHANGID` | VARCHAR(36) |  |

## Danh muc (Dictionary)

### <a name="dcalamviec"></a>DCALAMVIEC

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | VARCHAR(255) |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |
| `TILELUONG` | BIGINT |  |

### <a name="dchitieuchitiet"></a>DCHITIEUCHITIET

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DCHITIEUDOANHTHUID` | VARCHAR(36) |  |
| `DTINHTHANHID` | VARCHAR(36) |  |
| `CHITIEU` | BIGINT |  |

### <a name="dchitieudoanhthu"></a>DCHITIEUDOANHTHU

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `DNHOMCHITIEUDOANHTHUID` | VARCHAR(36) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `NAM` | INTEGER |  |
| `THANG1` | BIGINT |  |
| `THANG2` | BIGINT |  |
| `THANG3` | BIGINT |  |
| `THANG4` | BIGINT |  |
| `THANG5` | BIGINT |  |
| `THANG6` | BIGINT |  |
| `THANG7` | BIGINT |  |
| `THANG8` | BIGINT |  |
| `THANG9` | BIGINT |  |
| `THANG10` | BIGINT |  |
| `THANG11` | BIGINT |  |
| `THANG12` | BIGINT |  |

### <a name="dcuahang"></a>DCUAHANG

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | VARCHAR(255) |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |
| `DIACHI` | VARCHAR(255) |  |
| `DIENTHOAI` | VARCHAR(255) |  |
| `DKHOHANGID` | VARCHAR(36) |  |
| `STEMPLATEID` | VARCHAR(36) |  |
| `CODE` | VARCHAR(255) |  |

### <a name="ddinhluong"></a>DDINHLUONG

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DVATTUID` | VARCHAR(36) |  |
| `SOLUONG` | BIGINT |  |
| `DMATHANGID` | VARCHAR(36) |  |

### <a name="ddoitackygui"></a>DDOITACKYGUI

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | VARCHAR(255) |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |
| `HOAHONG` | BIGINT |  |

### <a name="ddonvichuyenphat"></a>DDONVICHUYENPHAT

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | VARCHAR(255) |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |

### <a name="ddonvitinh"></a>DDONVITINH

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `NOTE` | VARCHAR(255) |  |
| `NAME` | VARCHAR(255) | NOT NULL |
| `ID` | VARCHAR(36) | NOT NULL |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `SORTORDER` | VARCHAR(255) |  |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |

### <a name="ddotkhuyenmai"></a>DDOTKHUYENMAI

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DLOAIHINHKHUYENMAIID` | VARCHAR(36) |  |
| `TUNGAY` | TIMESTAMP |  |
| `DENNGAY` | TIMESTAMP |  |
| `NGUNGAPDUNG` | INTEGER |  |
| `TILEGIAMGIA` | BIGINT |  |

### <a name="ddotkhuyenmaichitiet"></a>DDOTKHUYENMAICHITIET

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DDOTKHUYENMAIID` | VARCHAR(36) |  |
| `DNHOMMATHANGID` | VARCHAR(36) |  |
| `TILEGIAMGIA` | BIGINT |  |
| `DMATHANGID` | VARCHAR(36) |  |
| `GIABAN` | BIGINT |  |
| `GIATRIDONHANG` | BIGINT |  |
| `SOLUONGMATHANG` | BIGINT |  |
| `SOLUONGMUA` | BIGINT |  |
| `SOLUONGTANG` | BIGINT |  |
| `DMATHANGTANGID` | VARCHAR(36) |  |

### <a name="dhangsanxuat"></a>DHANGSANXUAT

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | VARCHAR(255) |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |

### <a name="dkhachhang"></a>DKHACHHANG

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `DNHOMKHACHHANGID` | VARCHAR(36) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `MAKHACH` | VARCHAR(255) |  |
| `DIACHI` | VARCHAR(255) |  |
| `DIENTHOAI` | VARCHAR(255) |  |
| `EMAIL` | VARCHAR(255) |  |
| `MASOTHUE` | VARCHAR(255) |  |
| `DNHANVIENID` | VARCHAR(36) |  |
| `NGAYSINH` | TIMESTAMP |  |
| `DIEMTICHLUYBANDAU` | INTEGER |  |
| `GIABAN` | INTEGER |  |
| `DTINHTHANHID` | VARCHAR(36) |  |
| `FACEBOOK` | VARCHAR(255) |  |
| `DIACHIGIAOHANG` | VARCHAR(255) |  |
| `DNHACUNGCAPID` | VARCHAR(36) |  |
| `NGUOIDAIDIEN` | VARCHAR(255) |  |
| `CHUCVU` | VARCHAR(255) |  |
| `SONGAYCHONO` | INTEGER |  |

### <a name="dkhohang"></a>DKHOHANG

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | VARCHAR(255) |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |
| `CHOPHEPAMKHO` | INTEGER |  |
| `DCUAHANGID` | VARCHAR(36) |  |

### <a name="dloaihinhkhuyenmai"></a>DLOAIHINHKHUYENMAI

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | VARCHAR(255) |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |

### <a name="dloaihopdong"></a>DLOAIHOPDONG

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | VARCHAR(255) |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |

### <a name="dloaiquy"></a>DLOAIQUY

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | VARCHAR(255) |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |

### <a name="dlydothuchi"></a>DLYDOTHUCHI

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `NOTE` | VARCHAR(255) |  |
| `NAME` | VARCHAR(255) | NOT NULL |
| `LALYDOTHU` | INTEGER |  |
| `ID` | VARCHAR(36) | NOT NULL |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `SORTORDER` | VARCHAR(255) |  |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |
| `LOAILYDO` | INTEGER |  |

### <a name="dlydothuongphat"></a>DLYDOTHUONGPHAT

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | VARCHAR(255) |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |

### <a name="dmathang"></a>DMATHANG

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `NOTE` | VARCHAR(255) |  |
| `NAME` | VARCHAR(255) | NOT NULL |
| `CODE` | VARCHAR(255) |  |
| `GIANHAP` | BIGINT |  |
| `ID` | VARCHAR(36) | NOT NULL |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `GIABAN` | BIGINT |  |
| `GIABAN2` | BIGINT |  |
| `GIABAN3` | BIGINT |  |
| `GIABAN4` | BIGINT |  |
| `TONTOITHIEU` | BIGINT |  |
| `TONTOIDA` | BIGINT |  |
| `BAOHANH` | VARCHAR(255) |  |
| `MASANCO` | VARCHAR(255) |  |
| `DHANGSANXUATID` | VARCHAR(36) |  |
| `ANH` | BLOB |  |
| `HOAHONG` | BIGINT |  |
| `GIAVON` | BIGINT |  |
| `DDONVITINHCHANID` | VARCHAR(36) |  |
| `QUYDOI` | INTEGER |  |
| `GIABANCHAN` | BIGINT |  |
| `DDOITACKYGUIID` | VARCHAR(36) |  |
| `MACDINHGIAMGIA` | BIGINT |  |
| `MACDINHGIAMTIEN` | BIGINT |  |
| `DNHOMMATHANGID` | VARCHAR(36) |  |
| `DDONVITINHID` | VARCHAR(36) |  |
| `GIABAN5` | BIGINT |  |
| `GIABAN6` | BIGINT |  |
| `GIABAN7` | BIGINT |  |
| `GIABAN8` | BIGINT |  |
| `GIABAN9` | BIGINT |  |
| `GIABAN10` | BIGINT |  |
| `SOLUONG3` | BIGINT |  |
| `SOLUONG4` | BIGINT |  |
| `SOLUONG5` | BIGINT |  |
| `SOLUONG6` | BIGINT |  |
| `SOLUONG7` | BIGINT |  |
| `SOLUONG8` | BIGINT |  |
| `SOLUONG9` | BIGINT |  |
| `SOLUONG10` | BIGINT |  |
| `XUATXU` | VARCHAR(255) |  |
| `CHATLIEU` | VARCHAR(255) |  |
| `MAUSAC` | VARCHAR(255) |  |
| `KIEUDANG` | VARCHAR(255) |  |
| `LOAIDINHLUONG` | INTEGER |  |
| `KHOILUONG` | BIGINT |  |
| `GIABANLON` | BIGINT |  |
| `DDONVITINHLONID` | VARCHAR(36) |  |
| `QUYDOILON` | BIGINT |  |
| `GIANHAPLON` | BIGINT |  |
| `GIANHAPCHAN` | BIGINT |  |
| `COHANSUDUNG` | INTEGER |  |
| `COSERI` | INTEGER |  |
| `GIABANSILON` | BIGINT |  |
| `GIABANSICHAN` | BIGINT |  |

### <a name="dnhacungcap"></a>DNHACUNGCAP

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `DNHOMNHACUNGCAPID` | VARCHAR(36) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `MANHACUNGCAP` | VARCHAR(255) |  |
| `DIACHI` | VARCHAR(255) |  |
| `DIENTHOAI` | VARCHAR(255) |  |
| `EMAIL` | VARCHAR(255) |  |
| `WEBSITE` | VARCHAR(255) |  |

### <a name="dnhanvien"></a>DNHANVIEN

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | VARCHAR(255) |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |
| `CACHTINHLUONG` | INTEGER |  |
| `NGHITHU7` | INTEGER |  |
| `NGHICHUNHAT` | INTEGER |  |
| `LUONGCA` | BIGINT |  |
| `LUONGTHANG` | BIGINT |  |
| `DIACHI` | VARCHAR(255) |  |
| `DIENTHOAI` | VARCHAR(255) |  |
| `PHUCAP` | BIGINT |  |

### <a name="dnhomchitieudoanhthu"></a>DNHOMCHITIEUDOANHTHU

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | VARCHAR(255) |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |

### <a name="dnhomkhachhang"></a>DNHOMKHACHHANG

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | VARCHAR(255) |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |
| `TILEGIAMGIA` | BIGINT |  |
| `DIEMTICHLUY` | BIGINT |  |

### <a name="dnhommathang"></a>DNHOMMATHANG

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `NOTE` | VARCHAR(255) |  |
| `NAME` | VARCHAR(255) | NOT NULL |
| `ID` | VARCHAR(36) | NOT NULL |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `SORTORDER` | VARCHAR(255) |  |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |
| `CODE` | VARCHAR(255) |  |

### <a name="dnhomnhacungcap"></a>DNHOMNHACUNGCAP

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | VARCHAR(255) |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |

### <a name="dnhomykienkhachhang"></a>DNHOMYKIENKHACHHANG

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | VARCHAR(255) |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |

### <a name="dtaikhoannganhang"></a>DTAIKHOANNGANHANG

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | VARCHAR(255) |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |

### <a name="dtinhthanh"></a>DTINHTHANH

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | VARCHAR(255) |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |

### <a name="dtinhtranggoi"></a>DTINHTRANGGOI

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | VARCHAR(255) |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |

### <a name="dtrangthaihopdong"></a>DTRANGTHAIHOPDONG

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `SORTORDER` | VARCHAR(255) |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `PARENTID` | VARCHAR(36) |  |
| `PARENTDIR` | VARCHAR(255) |  |
| `ITEMTYPE` | INTEGER |  |
| `AUTOID` | BIGINT |  |
| `SIMAGEID` | VARCHAR(36) |  |

### <a name="dxegiaohang"></a>DXEGIAOHANG

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `BIENSO` | VARCHAR(255) |  |
| `BENXE` | VARCHAR(255) |  |
| `DIENTHOAILAIXE` | VARCHAR(255) |  |
| `DIENTHOAINHAXE` | VARCHAR(255) |  |
| `TUYENDUONG` | VARCHAR(255) |  |

### <a name="dykienkhachhang"></a>DYKIENKHACHHANG

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `DNHOMYKIENKHACHHANGID` | VARCHAR(36) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DKHACHHANGID` | VARCHAR(36) |  |
| `NGAY` | TIMESTAMP |  |
| `DMATHANGID` | VARCHAR(36) |  |

## Nghiep vu Ban hang va Dat hang

### <a name="tdonhang"></a>TDONHANG

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `NGAY` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `GIAMTHEOTIEN` | INTEGER |  |
| `LOAI` | INTEGER |  |
| `TONGCONG` | BIGINT |  |
| `PHIVANCHUYEN` | BIGINT |  |
| `TIENGIAMGIA` | BIGINT |  |
| `TILEGIAMGIA` | BIGINT |  |
| `TIENTHUE` | BIGINT |  |
| `TILETHUE` | BIGINT |  |
| `TIENHANG` | BIGINT |  |
| `DNHACUNGCAPID` | VARCHAR(36) |  |
| `DKHOXUATID` | VARCHAR(36) |  |
| `DKHONHAPID` | VARCHAR(36) |  |
| `DNHANVIENXUATID` | VARCHAR(36) |  |
| `DNHANVIENNHAPID` | VARCHAR(36) |  |
| `DIENGIAI` | VARCHAR(255) |  |
| `GIOTHANHTOAN` | TIMESTAMP |  |
| `KHACHDUA` | BIGINT |  |
| `TRALAI` | BIGINT |  |
| `NOCU` | BIGINT |  |
| `LANIN` | INTEGER |  |
| `LOAIGIA` | INTEGER |  |
| `USERTHANHTOANID` | VARCHAR(36) |  |
| `TIENTHANHTOAN` | BIGINT |  |
| `LOAITHANHTOAN` | INTEGER |  |
| `TDATHANGID` | VARCHAR(36) |  |
| `GIAOHANG` | VARCHAR(255) |  |
| `DATHANHTOAN` | INTEGER |  |
| `DOITRA` | BIGINT |  |
| `CONNO` | INTEGER |  |
| `DIEM` | BIGINT |  |
| `VOUCHER` | BIGINT |  |
| `DNHANVIENGIAOID` | VARCHAR(36) |  |
| `TRICHNHANVIEN` | BIGINT |  |
| `DCUAHANGID` | VARCHAR(36) |  |
| `CONLAI` | BIGINT |  |
| `THANHTOAN` | BIGINT |  |
| `DKHACHHANGID` | VARCHAR(36) |  |
| `DTAIKHOANNGANHANGID` | VARCHAR(36) |  |
| `MODE` | INTEGER |  |
| `DAXUATHOADON` | INTEGER |  |
| `SOHOADON` | VARCHAR(36) |  |
| `TGUIHANGCODID` | VARCHAR(36) |  |
| `TKETQUAGUICODID` | VARCHAR(36) |  |
| `GIATRIKETQUA` | INTEGER |  |
| `TDATHANGNCCID` | VARCHAR(36) |  |
| `TONGKHOILUONG` | BIGINT |  |

### <a name="tdonhangchitiet"></a>TDONHANGCHITIET

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `TDONHANGID` | VARCHAR(36) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DMATHANGID` | VARCHAR(36) |  |
| `BAOHANH` | VARCHAR(255) |  |
| `TILEGIAMGIA` | BIGINT |  |
| `THANHTIEN` | BIGINT |  |
| `DONGIA` | BIGINT |  |
| `SLNHAP` | BIGINT |  |
| `SLXUAT` | BIGINT |  |
| `SLTHUCTE` | BIGINT |  |
| `SLHETHONG` | BIGINT |  |
| `DKHOHANGID` | VARCHAR(36) |  |
| `KHUYENMAI` | INTEGER |  |
| `DDONVITINHID` | VARCHAR(36) |  |
| `SLNHAPCHUAQUYDOI` | BIGINT |  |
| `SLXUATCHUAQUYDOI` | BIGINT |  |
| `GIAMTHEOTIEN` | INTEGER |  |
| `TIENGIAMGIA` | BIGINT |  |
| `TDONHANGTRAID` | VARCHAR(36) |  |
| `GIAGANNHAT` | BIGINT |  |
| `XUATVATTU` | INTEGER |  |
| `GIATRINHAP` | BIGINT |  |
| `GIATRIXUAT` | BIGINT |  |
| `DONGIABAOCAO` | BIGINT |  |
| `GIAVON` | BIGINT |  |
| `QUYDOI` | BIGINT |  |
| `HANSUDUNG` | TIMESTAMP |  |
| `SERI` | VARCHAR(64) |  |

### <a name="tdonhanghuy"></a>TDONHANGHUY

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `NGAY` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `KHACHHANG` | VARCHAR(255) |  |
| `NHANVEN` | VARCHAR(255) |  |
| `THUNGAN` | VARCHAR(255) |  |
| `DOITRA` | BIGINT |  |
| `DATHANHTOAN` | INTEGER |  |
| `GIOTHANHTOAN` | TIMESTAMP |  |
| `TRALAI` | BIGINT |  |
| `TIENHANG` | BIGINT |  |
| `TILETHUE` | BIGINT |  |
| `TIENTHUE` | BIGINT |  |
| `TILEGIAMGIA` | BIGINT |  |
| `TIENGIAMGIA` | BIGINT |  |
| `PHIVANCHUYEN` | BIGINT |  |
| `THANHTOANBOI` | VARCHAR(255) |  |
| `NGAYHUY` | TIMESTAMP |  |
| `GIOHUY` | TIMESTAMP |  |

### <a name="tdonhanghuychitiet"></a>TDONHANGHUYCHITIET

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `TDONHANGHUYID` | VARCHAR(36) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `MAHANG` | VARCHAR(255) |  |
| `TENHANG` | VARCHAR(255) |  |
| `DVT` | VARCHAR(255) |  |
| `DONGIA` | BIGINT |  |
| `THANHTIEN` | BIGINT |  |
| `SOLUONG` | BIGINT |  |

### <a name="tdathang"></a>TDATHANG

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `NOTE` | VARCHAR(255) |  |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NGAY` | TIMESTAMP |  |
| `TENKHACH` | VARCHAR(255) |  |
| `DIACHI` | VARCHAR(255) |  |
| `DIENTHOAI` | VARCHAR(255) |  |
| `EMAIL` | VARCHAR(255) |  |
| `TIENHANG` | BIGINT |  |
| `TILETHUE` | BIGINT |  |
| `TIENTHUE` | BIGINT |  |
| `TILEGIAMGIA` | BIGINT |  |
| `TIENGIAMGIA` | BIGINT |  |
| `PHIVANCHUYEN` | BIGINT |  |
| `TONGCONG` | BIGINT |  |
| `LOAI` | INTEGER |  |
| `GIAMTHEOTIEN` | INTEGER |  |
| `ID` | VARCHAR(36) | NOT NULL |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DKHACHHANGID` | VARCHAR(36) |  |
| `LOAIGIA` | INTEGER |  |

### <a name="tdathangchitiet"></a>TDATHANGCHITIET

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `NOTE` | VARCHAR(255) |  |
| `SOLUONG` | BIGINT |  |
| `DONGIA` | BIGINT |  |
| `THANHTIEN` | BIGINT |  |
| `TILEGIAMGIA` | BIGINT |  |
| `ID` | VARCHAR(36) | NOT NULL |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `BAOHANH` | VARCHAR(255) |  |
| `DDONVITINHID` | VARCHAR(36) |  |
| `TDATHANGID` | VARCHAR(36) |  |
| `DMATHANGID` | VARCHAR(36) |  |
| `GIABANGANNHAT` | BIGINT |  |

### <a name="tdathangncc"></a>TDATHANGNCC

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `NGAY` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DNHACUNGCAPID` | VARCHAR(36) |  |
| `GIAMTHEOTIEN` | INTEGER |  |
| `LOAI` | INTEGER |  |
| `TONGCONG` | BIGINT |  |
| `PHIVANCHUYEN` | BIGINT |  |
| `TIENGIAMGIA` | BIGINT |  |
| `TILEGIAMGIA` | BIGINT |  |
| `TIENTHUE` | BIGINT |  |
| `TILETHUE` | BIGINT |  |
| `TIENHANG` | BIGINT |  |

### <a name="tdathangnccchitiet"></a>TDATHANGNCCCHITIET

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `TDATHANGNCCID` | VARCHAR(36) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DMATHANGID` | VARCHAR(36) |  |
| `TDATHANGID` | VARCHAR(36) |  |
| `DDONVITINHID` | VARCHAR(36) |  |
| `BAOHANH` | VARCHAR(255) |  |
| `TILEGIAMGIA` | BIGINT |  |
| `THANHTIEN` | BIGINT |  |
| `DONGIA` | BIGINT |  |
| `SOLUONG` | BIGINT |  |
| `GIAMUAGANNHAT` | BIGINT |  |

### <a name="tchitietthanhtoan"></a>TCHITIETTHANHTOAN

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `TDONHANGID` | VARCHAR(36) |  |
| `TTHUCHIID` | VARCHAR(36) |  |
| `SOTIEN` | BIGINT |  |

### <a name="tthuchi"></a>TTHUCHI

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `NOTE` | VARCHAR(255) |  |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NGAY` | TIMESTAMP |  |
| `TENDOITUONG` | VARCHAR(255) |  |
| `DIACHI` | VARCHAR(255) |  |
| `LOAI` | INTEGER |  |
| `LOAIDOITUONG` | INTEGER |  |
| `DIENGIAI` | VARCHAR(255) |  |
| `CHUNGTUGOC` | VARCHAR(255) |  |
| `THU` | BIGINT |  |
| `CHI` | BIGINT |  |
| `ID` | VARCHAR(36) | NOT NULL |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DNHACUNGCAPID` | VARCHAR(36) |  |
| `CHUYENKHOAN` | INTEGER |  |
| `LATAMUNG` | INTEGER |  |
| `TBANGLUONGID` | VARCHAR(36) |  |
| `TDATHANGID` | VARCHAR(36) |  |
| `DCUAHANGID` | VARCHAR(36) |  |
| `LAPHIEUTHUCONGNO` | INTEGER |  |
| `KHONGTHAYDOICONGNO` | INTEGER |  |
| `DNHANVIENID` | VARCHAR(36) |  |
| `DKHACHHANGID` | VARCHAR(36) |  |
| `DLYDOTHUCHIID` | VARCHAR(36) |  |
| `DTAIKHOANNGANHANGID` | VARCHAR(36) |  |
| `TDATHANGNCCID` | VARCHAR(36) |  |
| `TTHUCHIHANGLOATID` | VARCHAR(36) |  |

### <a name="tthuchihangloat"></a>TTHUCHIHANGLOAT

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `NGAY` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DNHANVIENID` | VARCHAR(36) |  |
| `LAPHIEUTHU` | INTEGER |  |
| `TONGCONG` | BIGINT |  |
| `LOAI` | INTEGER |  |

### <a name="tdoino"></a>TDOINO

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `NGAY` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DKHACHHANGID` | VARCHAR(36) |  |
| `DNHANVIENID` | VARCHAR(36) |  |

### <a name="tbaogia"></a>TBAOGIA

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `NOTE` | VARCHAR(255) |  |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NGAY` | TIMESTAMP |  |
| `TENKHACH` | VARCHAR(255) |  |
| `DIACHI` | VARCHAR(255) |  |
| `DIENTHOAI` | VARCHAR(255) |  |
| `EMAIL` | VARCHAR(255) |  |
| `TIENHANG` | BIGINT |  |
| `TILETHUE` | BIGINT |  |
| `TIENTHUE` | BIGINT |  |
| `TILEGIAMGIA` | BIGINT |  |
| `TIENGIAMGIA` | BIGINT |  |
| `PHIVANCHUYEN` | BIGINT |  |
| `TONGCONG` | BIGINT |  |
| `LOAI` | INTEGER |  |
| `GIAMTHEOTIEN` | INTEGER |  |
| `ID` | VARCHAR(36) | NOT NULL |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DKHACHHANGID` | VARCHAR(36) |  |
| `LOAIGIA` | INTEGER |  |

### <a name="tbaogiachitiet"></a>TBAOGIACHITIET

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `NOTE` | VARCHAR(255) |  |
| `SOLUONG` | BIGINT |  |
| `DONGIA` | BIGINT |  |
| `THANHTIEN` | BIGINT |  |
| `TILEGIAMGIA` | BIGINT |  |
| `ID` | VARCHAR(36) | NOT NULL |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DDONVITINHID` | VARCHAR(36) |  |
| `TBAOGIAID` | VARCHAR(36) |  |
| `DMATHANGID` | VARCHAR(36) |  |

### <a name="tgiamtheonhom"></a>TGIAMTHEONHOM

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DNHOMKHACHHANGID` | VARCHAR(36) |  |
| `DNHOMMATHANGID` | VARCHAR(36) |  |
| `TILEGIAM` | BIGINT |  |

### <a name="ttanggiamdiem"></a>TTANGGIAMDIEM

| Cot | Kieu | Rang buoc |
|------|------|-----------|
| `ID` | VARCHAR(36) | NOT NULL |
| `NAME` | VARCHAR(255) | NOT NULL |
| `NOTE` | VARCHAR(255) |  |
| `STATUS` | INTEGER | NOT NULL |
| `USERMODIFIEDID` | VARCHAR(36) |  |
| `TIMEMODIFIED` | TIMESTAMP |  |
| `TIMECREATED` | TIMESTAMP |  |
| `NGAY` | TIMESTAMP |  |
| `USERCREATEDID` | VARCHAR(36) | NOT NULL |
| `DKHACHHANGID` | VARCHAR(36) |  |
| `DIEMTANG` | BIGINT |  |
| `DIEMGIAM` | BIGINT |  |
| `LYDO` | VARCHAR(255) |  |

