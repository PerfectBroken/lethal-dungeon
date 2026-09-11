import type {Metadata} from 'next';
import './globals.css';
export const metadata:Metadata={title:'致命地下城 · 山洞模型预览',description:'哥布林栖居洞穴的实时三维审阅：室内、总览与俯视。'};
export default function RootLayout({children}:Readonly<{children:React.ReactNode}>){return <html lang="zh-CN"><body>{children}</body></html>}
