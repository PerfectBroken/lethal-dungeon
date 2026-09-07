# WorldClock

## 用途与边界

维护一次探险的服务端纯时钟。已确认来源：需求规格第4节。加载屏障由未来房间模块负责，只有全员统一到达后才能调用Start；本时钟不知道队员数量，不声称验证了屏障。

## 必须遵守的规范

| ID | 规则及状态 |
|---|---|
| CLOCK-001 | 已确认：启动06:00，现实1秒=游戏1分钟；启动前不消耗时间 |
| CLOCK-002 | 推导：720秒为18:00，1080秒为午夜；不存在12分钟强退 |
| CLOCK-003 | 工程契约：Created→Running→Expired；Start重复调用无效果，包括到期后；不得重置 |
| CLOCK-004 | 工程契约：以注入的单调时间源计算，不按轮询次数计时；亚秒不丢失 |
| CLOCK-005 | 工程契约：经过时间到1080秒即到期；Poll返回一次DeadlineReachedNow脉冲，后续不重复；跨过截止的长时间停顿也必须到期 |
| CLOCK-006 | 工程契约：快照不可变；显示分钟向下取整，午夜用1440表示；到期快照停在1080秒/1440分钟，不开始新一天 |
| CLOCK-007 | 工程契约：null时间源抛ArgumentNullException(source)；负时间或观察到倒退抛InvalidOperationException，失败不改变已接受状态 |

时间精度为TimeSpan tick（100纳秒），不是引擎帧；以上工程契约细化已授权测试范围，不决定撤离与死亡同刻的处理优先级。

## 接口与依赖

- `IMonotonicTimeSource.Now: TimeSpan`：非负、单调不减的现实经过时间，可有任意非负起始偏移。生产源由后续服务端适配器提供。
- `ExpeditionClock(IMonotonicTimeSource source)`：构造时只校验引用，不采样时间。
- `Start(): void`：第一次采样时间并建立基准；重复调用直接无效果，不重新采样。
- `Poll(): ClockSnapshot`：未启动时不采样，返回HasStarted=false、Elapsed=0、GameMinute=360、HasExpired=false、DeadlineReachedNow=false。
- 启动后Poll采样源：Elapsed为自启动累计现实时间，最大1080秒；GameMinute为自游戏日00:00起的完整分钟；HasStarted一直为true。
- 首次到期Poll令HasExpired和DeadlineReachedNow为true；之后Poll返回HasExpired=true、DeadlineReachedNow=false，不再读取时间源。
- 负数/倒退失败不消耗截止脉冲，不改变基准或上次有效采样；随后有效输入可继续。

仅标准库依赖；串行调用，不使用系统日期、Timer、Task.Delay或Thread.Sleep。保存的旧快照不会因后续Poll而改变。

## 测试与执行

| ID | Domain.Tests中的用例组 |
|---|---|
| CLOCK-001 | BeforeStartDoesNotConsumeLoadingTime；StartUsesArrivalAsOrigin |
| CLOCK-002/006 | TimelineHasSpecifiedBoundaries；VeryLargeElapsedTimeExpiresSafely；OldSnapshotRemainsUnchanged |
| CLOCK-003 | DuplicateStartCannotResetTime；StartAfterExpiryCannotRestart |
| CLOCK-004 | PollFrequencyDoesNotChangeElapsedTime；FractionalTimeIsPreserved |
| CLOCK-005 | DeadlinePulseOccursOnce；JumpAcrossDeadlineEmitsPulse；ExpiredClockStopsSampling |
| CLOCK-007 | NullSourceIsRejected；NegativeStartIsRejectedAndCanRetry；BackwardTimeIsRejectedWithoutChangingState；NegativePollIsRejectedWithoutChangingState |

测试文件：工程根 `tests/Domain.Tests/WorldClockTests.cs`。使用手动时间源，瞬时跳转到边界，禁止真实等待。执行命令见根README。

## AI修改约束

不得引入暂停、额外安全期、12分钟自动失败、撤离读条、死亡顺序或怪物计时。本模块只报告截止；房间模块负责终止游戏和结算。若以后需要保存恢复，应先设计新契约与测试。

## 验证与已知限制

已完成Green实现，24个时钟测试通过。尚未验证真实服务器时间源、联机到达屏障、微信后台和帧卡顿；此处的时间跳跃仅验证纯时钟逻辑。结果见evidence。
