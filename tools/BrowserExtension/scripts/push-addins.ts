import { readFile, copyFile, mkdirSync, existsSync } from 'fs';
import path, { resolve, basename } from 'path';


const jsonFilePath = resolve(process.cwd(), 'dist/addins-index.json');
const targetDir = resolve(process.cwd(), '../../src/II.RPA.WebBrowser.Common/ExtensionScripts');


if (!existsSync(targetDir)) {
    mkdirSync(targetDir, { recursive: true });
}

interface Addin {
    enabled: boolean;
    entry: string;
    outputFileName: string;
}


readFile(jsonFilePath, 'utf8', (err, data) => {
    if (err) {
        console.error('读取 JSON 文件失败:', err);
        return;
    }

    try {
        const addins: Addin[] = JSON.parse(data);

        // 筛选 enabled == true 的脚本
        const enabledAddins = addins.filter((addin: Addin) => addin.enabled === true);

        enabledAddins.forEach((addin: Addin) => {
            const sourcePath = resolve(process.cwd(), "dist/scripts", addin.outputFileName);
            const targetPath = resolve(targetDir, basename(addin.outputFileName));
            console.log(`准备复制文件: ${addin.outputFileName} -> ${targetPath}`);

            copyFile(sourcePath, targetPath, (copyErr) => {
                if (copyErr) {
                    console.error(`😒 复制文件失败: ${addin.outputFileName}`, copyErr);
                } else {
                    console.log(`✅ 成功复制文件:${addin.outputFileName}`);
                }
            });
        });
    } catch (parseErr) {
        console.error('解析 JSON 文件失败:', parseErr);
    }
});