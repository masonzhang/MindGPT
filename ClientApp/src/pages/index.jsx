import {Input, Spin} from "antd";
import {useCallback, useState} from "react";
import {SendOutlined} from "@ant-design/icons";
import styles from '../index.less';

export default function HomePage() {
    const [ask, setAsk] = useState('');
    const [loading, setLoading] = useState(false);

    const onSearch = useCallback(async () => {
        // Your code logic here
    }, [ask]);

    return <div className={styles.ask}>
        <Spin spinning={loading}>
            <Input.Search size="large"
                          value={ask}
                          onSearch={onSearch}
                          placeholder="write your goal here"
                          enterButton={<SendOutlined/>}
            />
        </Spin>
        <div className={styles.ask}>
        </div>
    </div>
}
