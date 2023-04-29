import {useContext} from "react";
import {GlobalContext} from "@/global-context";


const DocsPage = () => {
    const { globalVariable, setGlobalVariable } = useContext(GlobalContext);

    return (
        <div>
            <p>This is docs.</p>
            <p>{globalVariable.appPath}</p>
        </div>
    );
};

export default DocsPage;
