// Shared model/viewer ballistic timing; time is elapsed review seconds.
export function waterState(d,time){const duration=Math.sqrt(2*(d.start[1]-d.end[1])/9.81),t=((time+d.phase)%d.period+d.period)%d.period;return {y:Math.max(d.end[1],d.start[1]-.5*9.81*t*t),falling:t<duration,impact:t>=duration&&t<duration+.8,rippleAge:t-duration};}
