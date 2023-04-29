import {Link, Outlet} from 'umi';
import styles from './index.less';
import {useState} from "react";
import {GlobalContext} from "@/global-context";

export default function Layout() {
    const [globalVariable, setGlobalVariable] = useState({});
    return (
        <GlobalContext.Provider value={{globalVariable, setGlobalVariable}}>
            <div className={styles.main}>
                <div className={styles.navs}>
                    <ul>
                        <li>
                            <Link to="/">General</Link>
                        </li>
                        <li>
                            <Link to="/docs">Docs</Link>
                        </li>
                        <li>
                            <a href="https://github.com/masonzhang/MindGPT" target="_blank" rel="noopener noreferrer">Github</a>
                        </li>
                    </ul>
                    <Outlet/>
                </div>
            </div>
        </GlobalContext.Provider>
    );
}
