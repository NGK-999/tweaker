# WinUtil (Chris Titus) coverage matrix — ApexTweaker

Data: 2026-07-25  
Fonte: [WinUtil tweaks.json](https://github.com/ChrisTitusTech/winutil/blob/main/config/tweaks.json) · [christitus.com/windows-tool](https://christitus.com/windows-tool/)  
Política: **reimplementação C#** no pipeline Apex (ledger/Demo). Não cola scripts PS1.  
Atribuição: inspired by WinUtil Essential / Advanced sets (CTT).

Status: `covered` | `gap-safe` | `gap-confirm` | `gap-dangerous` | `out-of-scope`

## Essential Tweaks

| CTT Id | Apex hoje | Status | Ação Apex |
|--------|-----------|--------|-----------|
| WPFTweaksRestorePoint | `CreateRestorePoint` | covered | Utilidades / pré-tweak |
| WPFTweaksDeleteTempFiles | `MarketUtilitiesService.CleanTemporaryFiles` | covered | Utilidades + bundle Essential |
| WPFTweaksDiskCleanup | limpeza temp (parcial) | gap-safe | Bundle Essential chama clean + nota; sem UI cleanmgr full |
| WPFTweaksTelemetry | Policy + `ApplyPolicyAndServiceTweaks` parcial | gap-safe | Completar registry CTT no bundle |
| WPFTweaksConsumerFeatures | `cloud-content.disable-consumer-experiences` | covered | Bundle reforça DWORD |
| WPFTweaksDeliveryOptimization | Policy + DODownloadMode | covered | Bundle idempotente |
| WPFTweaksLocation | lfsvc em Policy | gap-safe | ConsentStore Deny + lfsvc no bundle |
| WPFTweaksActivity | — | gap-safe | EnableActivityFeed / PublishUserActivities = 0 |
| WPFTweaksWidget | `widgets.*` catalog | gap-safe | TaskbarDa / widgets off no bundle |
| WPFTweaksDisableStoreSearch | search catalog parcial | gap-safe | DisableSearchBoxSuggestions |
| WPFTweaksDisableExplorerAutoDiscovery | — | gap-safe | Folder discovery off |
| WPFTweaksPreventDeviceMetadataFromNetwork | — | gap-safe | PreventDeviceMetadataFromNetwork=1 |
| WPFTweaksWPBT | — | gap-safe | DisableWpbtExecution=1 |
| WPFTweaksEndTaskOnTaskbar | — | gap-safe | TaskbarEndTask=1 |
| WPFTweaksRevertStartMenu | — | gap-confirm | FeatureManagement override (condicional) |
| WPFTweaksServices | Policy mass-disable agressivo | gap-confirm | Bundle: **subset** DiagTrack/MapsBroker/CscService Manual\|Disabled; sem WSearch/SysMain no Essential |
| WPFTweaksHiber | power hibernate idle | gap-confirm | HibernateEnabled=0; **skip laptop/bateria** |
| WPFTweaksDisableBitLocker | — | gap-dangerous | `dangerous.ctt-disable-bitlocker` — confirm only |

## Performance Plans

| CTT Id | Apex hoje | Status | Ação |
|--------|-----------|--------|------|
| WPFAddUltPerf | `ActivateUltimatePerformanceOrFallback` | covered | `ctt.ultimate-performance` → Energia |
| WPFRemoveUltPerf | — | out-of-scope | Rollback via Utilidades/ledger |

## Advanced CAUTION

| CTT Id | Apex hoje | Status | Ação |
|--------|-----------|--------|------|
| WPFTweaksDisableFSO | `ApplyGameFullscreenOptimizationsOff` | covered | Performance / GPU |
| WPFTweaksRemoveEdge | `dangerous.remove-edge` | covered | Dangerous confirm |
| WPFTweaksEdgeDebloat | Edge policies parcial | gap-confirm | Advanced bundle |
| WPFTweaksRemoveOneDrive | onedrive policy | gap-confirm | Advanced + NoOneDrive |
| WPFTweaksDisableBGapps | Background tweaks parcial | gap-safe | GlobalUserDisabled=1 no Advanced |
| WPFTweaksDisplay | `ApplyUiNoiseTweaks` | covered | UI noise |
| WPFTweaksDisableNotifications | — | gap-confirm | Advanced |
| WPFTweaksRemoveHomeAndGallery | — | gap-confirm | Advanced |
| WPFTweaksRightClickMenu | — | gap-confirm | Advanced (Win11 classic menu) |
| WPFTweaksStorage | `utility.storage-sense-off` | covered | Storage Sense |
| WPFTweaksReservedStorage | — | gap-confirm | Advanced (`Set-WindowsReservedStorageState` equiv via DISM/API note) |
| WPFTweaksIPv46 | — | gap-confirm | Advanced DisabledComponents=32 |
| WPFTweaksTeredo | — | gap-confirm | Advanced |
| WPFTweaksDisableIPv6 | — | gap-dangerous | Full IPv6 off — dangerous |
| WPFTweaksUTC | — | gap-confirm | RealTimeIsUniversal — Advanced |
| WPFTweaksWindowsAI | — | gap-confirm | Advanced |
| WPFTweaksBlockAdobeNet | hosts/firewall | gap-confirm | Advanced (hosts append curated) |
| WPFTweaksRazerBlock | — | gap-confirm | DisableCoInstallers |
| WPFTweaksDisableWarningForUnsignedRdp | — | gap-confirm | Advanced |
| WPFTweaksBraveDebloat | — | out-of-scope | Browser de terceiros |
| WPFOOSUbutton | — | out-of-scope | App externo |
| WPFchangedns | Rede avançada parcial | out-of-scope | DNS UI dedicada fora deste lote |

## Customize Preferences

| CTT Id | Status | Ação |
|--------|--------|------|
| WPFToggleGameMode | covered | GPU/Display / Game Mode |
| WPFToggleMouseAcceleration | covered | Input/USB |
| WPFToggleMultiplaneOverlay | covered | MPO module |
| WPFToggleDarkMode / HiddenFiles / ShowExt / Taskbar* / Bing / StickyKeys / LongPaths / NumLock / Scrollbars / Battery / Verbose / BSoD / Lockscreen / S3 / Standby / NewOutlook / WindowSnapping / LoginBlur / SettingsHome / StartRecommendations | out-of-scope | Preferências cosméticas — não pacote gaming Essential |

## Fora de escopo (produto)

WinGet Install · Win11 Creator/ISO · Updates Disable ALL · export JSON multi-PC · `irm \| iex`.

## Implementação neste lote

| API | Conteúdo |
|-----|----------|
| `TweakService.ApplyCttEssentialTweaks` | Gaps Essential Safe + subset serviços + hibernate condicional + clean temp |
| `TweakService.ApplyCttAdvancedTweaks` | Gaps Advanced confirm (sem remove Edge/IPv6 full/BitLocker) |
| `TweakService.ApplyCttDisableBitLocker` | Dangerous isolado |
| Catalog `ctt.*` / `dangerous.ctt-*` | Analyze + badges |
| Módulos UI | Botões Essential + Advanced (confirm) |
