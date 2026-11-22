@echo off
REM 无损跨版本升级Windows V1.0.0
REM 基于 M-M-C 的原始脚本重做, 中文版, 个人网站 zhcool.xyz
REM 强制使用简体中文代码页, 避免中文系统下出现乱码或报错
chcp 936 >nul 2>&1
title 无损跨版本升级Windows V1.0.0

REM ===== 获取管理员权限 =====
>nul 2>&1 fsutil dirty query %systemdrive% && (goto gotAdmin) || (goto UACPrompt)
:UACPrompt
if exist "%SYSTEMROOT%\System32\Cscript.exe" (
    echo Set UAC = CreateObject^("Shell.Application"^) : UAC.ShellExecute "%~s0", "", "", "runas", 1 > "%temp%\getadmin.vbs"
    "%temp%\getadmin.vbs"
    exit /b
) else (
    powershell -Command "Start-Process -Verb RunAs -FilePath '%0' -ArgumentList 'am_admin'"
    exit /b
)
:gotAdmin
if exist "%temp%\getadmin.vbs" del "%temp%\getadmin.vbs"
pushd "%CD%" && CD /D "%~dp0"

SETLOCAL EnableDelayedExpansion
set "choice="
set "sourcespath="
set "srcdrive="
set "isopath="
set "productkey="
set "targetname="
set "editionid="
set "productname="
set "compositioneditionid="

REM ===== 读取当前系统版本 =====
set "curproductname="
set "cureditionid="
set "curcompositioneditionid="
for /f "tokens=2*" %%i in ('reg query "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion" /v ProductName 2^>nul') do set "curproductname=%%j"
for /f "tokens=2*" %%i in ('reg query "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion" /v EditionID 2^>nul') do set "cureditionid=%%j"
for /f "tokens=2*" %%i in ('reg query "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion" /v CompositionEditionID 2^>nul') do set "curcompositioneditionid=%%j"
if not defined curproductname set "curproductname=Windows 10 Pro"
if not defined cureditionid set "cureditionid=Professional"
if not defined curcompositioneditionid set "curcompositioneditionid=Enterprise"

REM ===== 第一步: 确定安装源 =====
call :detectsource
if defined sourcespath goto editionmenu
goto sourcemenu

REM ============================================================
REM  安装源选择
REM ============================================================
:sourcemenu
cls
echo.
echo  ============================================================
echo   无损跨版本升级Windows  V1.0.0
echo  ============================================================
echo.
echo  第一步: 选择安装介质
echo  介质需要同时包含 setup.exe 和 sources 文件夹.
echo.
call :scanmedia
if !foundcount! gtr 0 goto sourcelist
echo  没有扫描到可用的安装介质, 请用下面的方式指定.
echo.
goto sourceactions

:sourcelist
echo  扫描到 !foundcount! 个可用的安装介质:
echo.
for /l %%i in (1,1,!foundcount!) do (
    echo    %%i. !srcpath%%i!
)
echo.

:sourceactions
echo   F. 浏览文件夹           I. 浏览 ISO 镜像并自动挂载
echo   M. 手动粘贴路径         R. 重新扫描
echo   0. 退出
echo.
echo   也可以直接把文件夹或 ISO 拖进本窗口, 或在下面粘贴路径.
echo.
set "pick="
set /p "pick=请输入编号或选项: "
if not defined pick goto sourcemenu
if "!pick!"=="0" goto endofbatch
if /i "!pick!"=="f" goto browsefolder
if /i "!pick!"=="i" goto browseiso
if /i "!pick!"=="m" goto manualsource
if /i "!pick!"=="r" goto sourcemenu
set "isnum=1"
for /f "delims=0123456789" %%a in ("!pick!") do set "isnum=0"
if "!isnum!"=="0" goto sourcepathinput
if !pick! lss 1 goto sourcebad
if !pick! gtr !foundcount! goto sourcebad
call set "sourcespath=%%srcpath!pick!%%"
if not defined sourcespath goto sourcebad
goto editionmenu

:sourcepathinput
set "pick=!pick:"=!"
call :normalizepath "!pick!"
if /i "!norm:~-4!"==".iso" goto manualiso
call :acceptdir "!norm!"
if not defined sourcespath goto sourcemenu
goto editionmenu

:sourcebad
echo.
echo  无效选项或编号, 请重新输入.
timeout /t 1 >nul
goto sourcemenu

:scanmedia
set "foundcount=0"
for %%d in (C D E F G H I J K L M N O P Q R S T U V W X Y Z) do call :addmedia "%%d:\"
if exist "%~dp0setup.exe" if exist "%~dp0sources\" call :addmedia "%~dp0"
exit /b 0

:addmedia
if !foundcount! gtr 0 (
    for /l %%k in (1,1,!foundcount!) do if /i "!srcpath%%k!"=="%~1" exit /b 0
)
if not exist "%~1setup.exe" exit /b 0
if not exist "%~1sources\" exit /b 0
set /a foundcount+=1
set "srcpath!foundcount!=%~1"
exit /b 0

:browsefolder
echo.
del "%temp%\upgradetool_pick.txt" >nul 2>&1
echo  正在打开文件夹选择窗口, 请稍候...
powershell -NoProfile -STA -Command "Add-Type -AssemblyName System.Windows.Forms; $d=New-Object System.Windows.Forms.FolderBrowserDialog; $d.Description='选择包含 setup.exe 和 sources 文件夹的安装介质目录'; $d.ShowNewFolderButton=$false; if($d.ShowDialog() -eq 'OK'){[IO.File]::WriteAllText('%temp%\upgradetool_pick.txt', $d.SelectedPath, [Text.Encoding]::Default)}"
if not exist "%temp%\upgradetool_pick.txt" (
    echo.
    echo  没有取到选择结果, 可以改用 M 手动粘贴路径.
    pause
    goto sourcemenu
)
set "picked="
set /p "picked=" < "%temp%\upgradetool_pick.txt"
del "%temp%\upgradetool_pick.txt" >nul 2>&1
if not defined picked goto sourcemenu
call :acceptdir "!picked!"
if not defined sourcespath goto sourcemenu
goto editionmenu

:browseiso
echo.
del "%temp%\upgradetool_pick.txt" >nul 2>&1
echo  正在打开 ISO 选择窗口, 请稍候...
powershell -NoProfile -STA -Command "Add-Type -AssemblyName System.Windows.Forms; $d=New-Object System.Windows.Forms.OpenFileDialog; $d.Title='选择 Windows 安装 ISO'; $d.DefaultExt='iso'; $d.Filter=('ISO 镜像'+[char]124+'*.iso'+[char]124+'所有文件'+[char]124+'*.*'); if($d.ShowDialog() -eq 'OK'){[IO.File]::WriteAllText('%temp%\upgradetool_pick.txt', $d.FileName, [Text.Encoding]::Default)}"
if not exist "%temp%\upgradetool_pick.txt" (
    echo.
    echo  没有取到选择结果, 可以改用 M 手动粘贴路径.
    pause
    goto sourcemenu
)
set "picked="
set /p "picked=" < "%temp%\upgradetool_pick.txt"
del "%temp%\upgradetool_pick.txt" >nul 2>&1
if not defined picked goto sourcemenu
call :mountiso "!picked!"
if not defined sourcespath goto sourcemenu
goto editionmenu

:acceptdir
call :normalizepath "%~1"
if "!norm:~-1!"=="\" (set "cand=!norm!") else (set "cand=!norm!\")
if not exist "!cand!setup.exe" goto acceptbad
if not exist "!cand!sources\" goto acceptbad
set "sourcespath=!cand!"
echo.
echo  已选择安装源: !sourcespath!
exit /b 0

:acceptbad
echo.
echo  该目录下没有 setup.exe 和 sources 文件夹, 请重新指定.
pause
exit /b 1

:manualsource
cls
echo.
echo  请输入安装介质路径, 也可以把文件夹直接拖进本窗口后回车.
echo  例如: F:\   或   D:\Win11_26H2\   或   D:\ISO\Win11.iso
echo  输入 0 返回.
echo.
set "src="
set /p "src=路径: "
if not defined src goto sourcemenu
if "!src!"=="0" goto sourcemenu
set "src=!src:"=!"
call :normalizepath "!src!"
if /i "!norm:~-4!"==".iso" goto manualiso
call :acceptdir "!norm!"
if not defined sourcespath goto manualsource
goto editionmenu

:manualiso
call :mountiso "!norm!"
if not defined sourcespath goto sourcemenu
goto editionmenu

REM ============================================================
REM  第二步: 选择目标版本
REM ============================================================
:editionmenu
cls
call :showstatus
echo  一、选择目标版本
echo  ------------------------------------------------------------
echo.
echo   1) Windows 家庭版                        11) Windows 家庭版 N
echo   2) Windows 专业版                        12) Windows 专业版 N
echo   3) Windows 专业工作站版                  13) Windows 专业工作站版 N
echo   4) Windows 企业版                        14) Windows 专业教育版 N
echo   5) Windows 专业教育版                    15) Windows 教育版 N
echo   6) Windows 教育版                        16) Windows 企业版 N
echo   7) Windows 企业版 多会话版               17) Windows 11 SE 云版 N
echo   8) Windows IoT 企业版
echo   9) Windows 家庭版 单语言版
echo   10) Windows 11 SE 云版
echo.
echo   LTSC 和 Server 版本需要对应的独立安装介质:
echo   18) Windows 10 企业版 LTSC 2021          21) Windows Server 2022 标准版
echo   19) Windows 10 IoT 企业版 LTSC 2021      22) Windows Server 2022 数据中心版
echo   20) Windows 10 企业版 N LTSC 2021
echo   23) Windows 11 企业版 LTSC 2024          26) Windows Server 2025 标准版
echo   24) Windows 11 IoT 企业版 LTSC 2024      27) Windows Server 2025 数据中心版
echo   25) Windows 11 企业版 N LTSC 2024
echo.
echo   28) 自定义版本, 自己提供预安装密钥(列表里没有的版本用这个)
echo.
echo   0) 退出
echo.
set "choice="
set /p "choice=请输入版本编号: "
if not defined choice goto editionmenu
if "!choice!"=="0" goto endofbatch
set "isnum=1"
for /f "delims=0123456789" %%a in ("!choice!") do set "isnum=0"
if "!isnum!"=="0" goto editionbad
if !choice! lss 1 goto editionbad
if !choice! gtr 28 goto editionbad
if "!choice!"=="28" goto customedition
if "!choice!"=="1" goto setvarcore
if "!choice!"=="2" goto setvarpro
if "!choice!"=="3" goto setvarpfw
if "!choice!"=="4" goto setvarent
if "!choice!"=="5" goto setvarproed
if "!choice!"=="6" goto setvared
if "!choice!"=="7" goto setvarentmulti
if "!choice!"=="8" goto setvariotent
if "!choice!"=="9" goto setvarcoresl
if "!choice!"=="10" goto setvarcloud
if "!choice!"=="11" goto setvarcoren
if "!choice!"=="12" goto setvarpron
if "!choice!"=="13" goto setvarpfwn
if "!choice!"=="14" goto setvarproedn
if "!choice!"=="15" goto setvaredn
if "!choice!"=="16" goto setvarentn
if "!choice!"=="17" goto setvarcloudn
if "!choice!"=="18" goto setvarltsc2021
if "!choice!"=="19" goto setvariotltsc2021
if "!choice!"=="20" goto setvarltscn2021
if "!choice!"=="21" goto setvarserv22std
if "!choice!"=="22" goto setvarserv22data
if "!choice!"=="23" goto setvarltsc2024
if "!choice!"=="24" goto setvariotltsc2024
if "!choice!"=="25" goto setvarltscn2024
if "!choice!"=="26" goto setvarserv25std
if "!choice!"=="27" goto setvarserv25data
goto editionbad

:editionbad
echo.
echo  无效的版本编号, 请重新输入.
timeout /t 1 >nul
goto editionmenu

:customedition
cls
echo.
echo  自定义版本
echo  ------------------------------------------------------------
echo  适用于版本列表里没有的版本, 需要你自备 25 位预安装密钥(GLVK).
echo  这类密钥只用于选择安装版本, 不能激活系统.
echo.
echo  输入 0 返回版本列表.
echo.
set "editionid="
set "compositioneditionid="
set "productname=!curproductname!"
set "ckey="
set /p "ckey=密钥(可带或不带连字符): "
if not defined ckey goto editionmenu
if "!ckey!"=="0" goto editionmenu
call :formatkey "!ckey!"
if not defined productkey goto customkeybad
echo.
echo  已识别密钥: !productkey!
echo.
echo  下面两项用于方法 4(强制升级)伪装版本, 方法 1 和方法 3 可以跳过.
echo  EditionID 例如: Core / Professional / Enterprise / EnterpriseS
set "cedition="
set /p "cedition=EditionID(直接回车跳过): "
if defined cedition set "editionid=!cedition!"
if defined cedition set "compositioneditionid=!cedition!"
echo  ProductName 例如: Windows 11 Pro
set "cproduct="
set /p "cproduct=ProductName(直接回车使用当前系统值 !curproductname!): "
if defined cproduct set "productname=!cproduct!"
set "targetname=自定义版本"
goto modemenu

:customkeybad
echo.
echo  密钥格式不正确, 去掉空格和连字符后应该是 25 位字母或数字.
pause
goto customedition

:formatkey
set "productkey="
set "raw=%~1"
set "raw=!raw: =!"
set "raw=!raw:-=!"
set "len=0"
for /l %%a in (0,1,40) do if not "!raw:~%%a,1!"=="" set /a len=%%a+1
if not "!len!"=="25" exit /b 1
set "productkey=!raw:~0,5!-!raw:~5,5!-!raw:~10,5!-!raw:~15,5!-!raw:~20,5!"
exit /b 0

REM ============================================================
REM  第三步: 选择执行方式
REM ============================================================
:modemenu
cls
call :showstatus
echo  二、选择执行方式
echo  ------------------------------------------------------------
echo.
echo  目标版本: !targetname!
if defined editionid (echo  版本标识: !editionid!) else (echo  版本标识: 未指定, 仅方法 1 / 3 可用)
echo  预安装密钥: !productkey!
echo.
echo   k) 仅更换密钥     用 slmgr 直接安装所选密钥, 只切换版本, 不做就地升级
echo   s) 普通升级       不指定版本, 由安装程序自行判断, 等同普通就地升级
echo   u) 指定版本升级   就地升级到所选版本, 自动使用对应的预安装密钥
echo   f) 强制升级       改写注册表伪装版本, 可跨过官方升级路径限制
echo.
echo   0) 返回版本列表
echo.
set "mode="
set /p "mode=请选择执行方式: "
if not defined mode goto modemenu
if /i "!mode!"=="0" goto resettarget
if /i "!mode!"=="k" goto keychange
if /i "!mode!"=="s" goto runboringupgrade
if /i "!mode!"=="u" goto runupgrade
if /i "!mode!"=="f" goto runforcedupgrade
echo.
echo  无效选项, 请重新输入.
timeout /t 1 >nul
goto modemenu

:resettarget
set "targetname="
set "productkey="
set "editionid="
set "productname="
set "compositioneditionid="
goto editionmenu

REM ============================================================
REM  执行
REM ============================================================
:keychange
cls
call :showstatus
echo.
echo  正在安装预安装密钥, 仅切换版本, 不做就地升级...
slmgr /ipk !productkey!
echo.
pause
goto modemenu

:runboringupgrade
cls
call :showstatus
echo.
echo  即将执行普通就地升级, 不指定目标版本.
echo  升级程序和后台进程正在运行, 请稍候. 完成后本窗口会自动关闭.
echo.
pause
"%sourcespath%\setup.exe" /eula accept /telemetry disable /priority normal /resizerecoverypartition enable
goto endofbatch

:runupgrade
cls
call :showstatus
echo.
echo  即将就地升级到 !targetname!.
echo  升级程序和后台进程正在运行, 请稍候. 完成后本窗口会自动关闭.
echo.
pause
"%sourcespath%\setup.exe" /eula accept /telemetry disable /priority normal /resizerecoverypartition enable /pkey !productkey!
goto endofbatch

:runforcedupgrade
cls
call :showstatus
if not defined editionid (
    echo.
    echo  自定义版本做强制升级需要 EditionID, 请回到版本列表重新填写.
    echo.
    pause
    goto modemenu
)
echo.
echo  即将强制升级到 !targetname!.
echo.
echo  原理是把注册表里的 ProductName / EditionID / CompositionEditionID
echo  改写为目标版本的值, 让安装程序以为当前已经是该版本.
echo  这样可以执行不在官方升级路径中的就地升级, 例如从专业版降级到家庭版,
echo  也可以解锁因授权原因被封锁的升级路径, 例如从家庭版直接升级到企业版.
echo.
echo  此方法完全不受微软支持, 风险自负. 建议先备份系统和重要数据.
echo  如果不小心写错了版本, 用正确的版本再执行一次强制升级即可覆盖回来.
echo.
set "confirm="
set /p "confirm=输入 Y 继续, 直接回车取消: "
if /i not "!confirm!"=="Y" goto modemenu
echo.
echo  正在写入注册表项...
Reg.exe add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion" /v "EditionID" /t REG_SZ /d "!editionid!" /f
Reg.exe add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion" /v "ProductName" /t REG_SZ /d "!productname!" /f
Reg.exe add "HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion" /v "CompositionEditionID" /t REG_SZ /d "!compositioneditionid!" /f
Reg.exe add "HKLM\SOFTWARE\Wow6432Node\Microsoft\Windows NT\CurrentVersion" /v "EditionID" /t REG_SZ /d "!editionid!" /f /reg:32
Reg.exe add "HKLM\SOFTWARE\Wow6432Node\Microsoft\Windows NT\CurrentVersion" /v "ProductName" /t REG_SZ /d "!productname!" /f /reg:32
Reg.exe add "HKLM\SOFTWARE\Wow6432Node\Microsoft\Windows NT\CurrentVersion" /v "CompositionEditionID" /t REG_SZ /d "!compositioneditionid!" /f /reg:32
echo.
echo  升级程序和后台进程正在运行, 请稍候. 完成后本窗口会自动关闭.
"%sourcespath%\setup.exe" /eula accept /telemetry disable /priority normal /resizerecoverypartition enable /pkey !productkey!
goto endofbatch

REM ============================================================
REM  辅助功能
REM ============================================================
:detectsource
if exist ".\setup.exe" if exist ".\sources\" (
    set "sourcespath=%CD%"
    exit /b 0
)
if exist "%~dp0setup.exe" if exist "%~dp0sources\" (
    set "sourcespath=%~dp0"
    exit /b 0
)
exit /b 1

:normalizepath
set "norm=%~1"
for %%a in ("!norm!") do set "norm=%%~a"
if "!norm:~-1!"=="\" if not "!norm:~1,2!"==":\" set "norm=!norm:~0,-1!"
exit /b 0

:mountiso
set "isopath=%~1"
if not exist "!isopath!" (
    echo.
    echo  找不到该 ISO 文件.
    exit /b 1
)
echo.
echo  正在挂载 ISO, 可能需要几秒钟...
set "srcdrive="
for /f "usebackq delims=" %%v in (`powershell -NoProfile -Command "$ErrorActionPreference='Stop'; $img=Mount-DiskImage -ImagePath '!isopath!' -PassThru; Start-Sleep -Milliseconds 800; $v=$img ^| Get-Volume; if($v){$v.DriveLetter}"`) do set "srcdrive=%%v"
if not defined srcdrive (
    echo.
    echo  挂载失败. 请确认选择的是 ISO 镜像, 或手动挂载后用编号 / F / M 重新指定.
    exit /b 1
)
set "sourcespath=!srcdrive!:\"
if not exist "!sourcespath!setup.exe" (
    echo.
    echo  已挂载到 !srcdrive!:, 但里面没有 setup.exe.
    exit /b 1
)
if not exist "!sourcespath!sources\" (
    echo.
    echo  已挂载到 !srcdrive!:, 但里面没有 sources 文件夹.
    exit /b 1
)
echo  已挂载到 !srcdrive!:, 并检测到安装文件.
exit /b 0

:showstatus
echo  ============================================================
echo   无损跨版本升级Windows  V1.0.0
echo   安装源: !sourcespath!
echo   当前系统: !curproductname! [!cureditionid!]
if defined targetname echo   已选版本: !targetname!
echo  ============================================================
echo.
exit /b 0

REM ============================================================
REM  以下定义各个 Windows 版本
REM ============================================================

REM Windows 家庭版
:setvarcore
set "productkey=YTMG3-N6DKC-DKB77-7M9GH-8HVX7"
set "editionid=Core"
set "productname=Windows 10 Home"
set "compositioneditionid=Core"
set "targetname=Windows 家庭版"
goto modemenu

REM Windows 专业版
:setvarpro
set "productkey=VK7JG-NPHTM-C97JM-9MPGT-3V66T"
set "editionid=Professional"
set "productname=Windows 10 Pro"
set "compositioneditionid=Enterprise"
set "targetname=Windows 专业版"
goto modemenu

REM Windows 专业工作站版
:setvarpfw
set "productkey=DXG7C-N36C4-C4HTG-X4T3X-2YV77"
set "editionid=ProfessionalWorkstation"
set "productname=Windows 10 Pro for Workstations"
set "compositioneditionid=Enterprise"
set "targetname=Windows 专业工作站版"
goto modemenu

REM Windows 企业版
:setvarent
set "productkey=XGVPP-NMH47-7TTHJ-W3FW7-8HV2C"
set "editionid=Enterprise"
set "productname=Windows 10 Enterprise"
set "compositioneditionid=Enterprise"
set "targetname=Windows 企业版"
goto modemenu

REM Windows 专业教育版
:setvarproed
set "productkey=8PTT6-RNW4C-6V7J2-C2D3X-MHBPB"
set "editionid=ProfessionalEducation"
set "productname=Windows 10 Pro Education"
set "compositioneditionid=Enterprise"
set "targetname=Windows 专业教育版"
goto modemenu

REM Windows 教育版
:setvared
set "productkey=YNMGQ-8RYV3-4PGQ3-C8XTP-7CFBY"
set "editionid=Education"
set "productname=Windows 10 Education"
set "compositioneditionid=Enterprise"
set "targetname=Windows 教育版"
goto modemenu

REM Windows 企业版 多会话版 / 虚拟桌面
:setvarentmulti
set "productkey=CPWHC-NT2C7-VYW78-DHDB2-PG3GK"
set "editionid=ServerRdsh"
set "productname=Windows 10 Enterprise multi-session"
set "compositioneditionid=Enterprise"
set "targetname=Windows 企业版 多会话版"
goto modemenu

REM Windows IoT 企业版
:setvariotent
set "productkey=XQQYW-NFFMW-XJPBH-K8732-CKFFD"
set "editionid=IoTEnterprise"
set "productname=Windows 10 IoT Enterprise"
set "compositioneditionid=Enterprise"
set "targetname=Windows IoT 企业版"
goto modemenu

REM Windows 家庭版 单语言版
:setvarcoresl
set "productkey=BT79Q-G7N6G-PGBYW-4YWX6-6F4BT"
set "editionid=CoreSingleLanguage"
set "productname=Windows 10 Home Single Language"
set "compositioneditionid=Core"
set "targetname=Windows 家庭版 单语言版"
goto modemenu

REM Windows SE 云版
:setvarcloud
set "productkey=KY7PN-VR6RX-83W6Y-6DDYQ-T6R4W"
set "editionid=CloudEdition"
set "productname=Windows 10 SE"
set "compositioneditionid=Enterprise"
set "targetname=Windows 11 SE 云版"
goto modemenu

REM Windows 家庭版 N
:setvarcoren
set "productkey=4CPRK-NM3K3-X6XXQ-RXX86-WXCHW"
set "editionid=CoreN"
set "productname=Windows 10 Home N"
set "compositioneditionid=CoreN"
set "targetname=Windows 家庭版 N"
goto modemenu

REM Windows 专业版 N
:setvarpron
set "productkey=2B87N-8KFHP-DKV6R-Y2C8J-PKCKT"
set "editionid=ProfessionalN"
set "productname=Windows 10 Pro N"
set "compositioneditionid=EnterpriseN"
set "targetname=Windows 专业版 N"
goto modemenu

REM Windows 专业工作站版 N
:setvarpfwn
set "productkey=WYPNQ-8C467-V2W6J-TX4WX-WT2RQ"
set "editionid=ProfessionalWorkstationN"
set "productname=Windows 10 Pro N for Workstations"
set "compositioneditionid=EnterpriseN"
set "targetname=Windows 专业工作站版 N"
goto modemenu

REM Windows 专业教育版 N
:setvarproedn
set "productkey=GJTYN-HDMQY-FRR76-HVGC7-QPF8P"
set "editionid=ProfessionalEducationN"
set "productname=Windows 10 Pro Education N"
set "compositioneditionid=EnterpriseN"
set "targetname=Windows 专业教育版 N"
goto modemenu

REM Windows 教育版 N
:setvaredn
set "productkey=84NGF-MHBT6-FXBX8-QWJK7-DRR8H"
set "editionid=EducationN"
set "productname=Windows 10 Education N"
set "compositioneditionid=EnterpriseN"
set "targetname=Windows 教育版 N"
goto modemenu

REM Windows 企业版 N
:setvarentn
set "productkey=3V6Q6-NQXCX-V8YXR-9QCYV-QPFCT"
set "editionid=EnterpriseN"
set "productname=Windows 10 Enterprise N"
set "compositioneditionid=EnterpriseN"
set "targetname=Windows 企业版 N"
goto modemenu

REM Windows SE 云版 N
:setvarcloudn
set "productkey=K9VKN-3BGWV-Y624W-MCRMQ-BHDCD"
set "editionid=CloudEditionN"
set "productname=Windows 10 SE N"
set "compositioneditionid=EnterpriseN"
set "targetname=Windows 11 SE 云版 N"
goto modemenu

REM Windows 10 企业版 LTSC 2021
:setvarltsc2021
set "productkey=M7XTQ-FN8P6-TTKYV-9D4CC-J462D"
set "editionid=EnterpriseS"
set "productname=Windows 10 Enterprise LTSC 2021"
set "compositioneditionid=EnterpriseS"
set "targetname=Windows 10 企业版 LTSC 2021"
goto modemenu

REM Windows 10 IoT 企业版 LTSC 2021
:setvariotltsc2021
set "productkey=QPM6N-7J2WJ-P88HH-P3YRH-YY74H"
set "editionid=IoTEnterpriseS"
set "productname=Windows 10 IoT Enterprise LTSC 2021"
set "compositioneditionid=EnterpriseS"
set "targetname=Windows 10 IoT 企业版 LTSC 2021"
goto modemenu

REM Windows 10 企业版 N LTSC 2021
:setvarltscn2021
set "productkey=2D7NQ-3MDXF-9WTDT-X9CCP-CKD8V"
set "editionid=EnterpriseSN"
set "productname=Windows 10 Enterprise N LTSC 2021"
set "compositioneditionid=EnterpriseSN"
set "targetname=Windows 10 企业版 N LTSC 2021"
goto modemenu

REM Windows Server 2022 标准版
:setvarserv22std
set "productkey=VDYBN-27WPP-V4HQT-9VMD4-VMK7H"
set "editionid=ServerStandard"
set "productname=Windows Server 2022 Standard"
set "compositioneditionid=ServerStandard"
set "targetname=Windows Server 2022 标准版"
goto modemenu

REM Windows Server 2022 数据中心版
:setvarserv22data
set "productkey=WX4NM-KYWYW-QJJR4-XV3QB-6VM33"
set "editionid=ServerDatacenter"
set "productname=Windows Server 2022 Datacenter"
set "compositioneditionid=ServerDatacenter"
set "targetname=Windows Server 2022 数据中心版"
goto modemenu

REM Windows 11 企业版 LTSC 2024
:setvarltsc2024
set "productkey=M7XTQ-FN8P6-TTKYV-9D4CC-J462D"
set "editionid=EnterpriseS"
set "productname=Windows 11 Enterprise LTSC"
set "compositioneditionid=EnterpriseS"
set "targetname=Windows 11 企业版 LTSC 2024"
goto modemenu

REM Windows 11 IoT 企业版 LTSC 2024
:setvariotltsc2024
set "productkey=CGK42-GYN6Y-VD22B-BX98W-J8JXD"
set "editionid=IoTEnterpriseS"
set "productname=Windows 11 IoT Enterprise LTSC"
set "compositioneditionid=EnterpriseS"
set "targetname=Windows 11 IoT 企业版 LTSC 2024"
goto modemenu

REM Windows 11 企业版 N LTSC 2024
:setvarltscn2024
set "productkey=92NFX-8DJQP-P6BBQ-THF9C-7CG2H"
set "editionid=EnterpriseSN"
set "productname=Windows 11 Enterprise N LTSC"
set "compositioneditionid=EnterpriseSN"
set "targetname=Windows 11 企业版 N LTSC 2024"
goto modemenu

REM Windows Server 2025 标准版
:setvarserv25std
set "productkey=TVRH6-WHNXV-R9WG3-9XRFY-MY832"
set "editionid=ServerStandard"
set "productname=Windows Server 2025 Standard"
set "compositioneditionid=ServerStandard"
set "targetname=Windows Server 2025 标准版"
goto modemenu

REM Windows Server 2025 数据中心版
:setvarserv25data
set "productkey=D764K-2NDRG-47T6Q-P8T8W-YP6DF"
set "editionid=ServerDatacenter"
set "productname=Windows Server 2025 Datacenter"
set "compositioneditionid=ServerDatacenter"
set "targetname=Windows Server 2025 数据中心版"
goto modemenu

:endofbatch
echo.
timeout /t 2 >nul
exit
