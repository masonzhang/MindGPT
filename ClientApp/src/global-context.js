import {createContext} from "react";

// initial all controllers by preload data
const links = ["ask", "notifications", "appSysInformation", "clipboard"];
links.forEach(async pageName => {
    const page = await fetch("api/" + pageName);
    console.log(`${pageName} loaded`);
});

export const GlobalContext = createContext({});
