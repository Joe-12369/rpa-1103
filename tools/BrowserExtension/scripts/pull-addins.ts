import { readdir, readFile, copyFile, mkdirSync, existsSync } from 'fs';
import { resolve, extname, basename } from 'path';

const targetDir = resolve(process.cwd(), '../../src/II.RPA.WebBrowser.Common/ExtensionScripts');
const jsonFilePath = resolve(process.cwd(), 'dist/addins-index.json');
const addinsDir = resolve(process.cwd(), 'src/addins');
const contentDir = resolve(addinsDir, 'content');

if (!existsSync(addinsDir)) {
    mkdirSync(addinsDir, { recursive: true });
}
if (!existsSync(contentDir)) {
    mkdirSync(contentDir, { recursive: true });
}

interface Addin {
    name: string;
    kind: string;
    entry: string;
}


readdir(targetDir, (err, files) => {
    if (err) {
        console.error('读取目标目录失败:', err);
        return;
    }

    readFile(jsonFilePath, 'utf8', (readErr, data) => {
        if (readErr) {
            console.error('读取 JSON 文件失败:', readErr);
            return;
        }

        try {
            const addins: Addin[] = JSON.parse(data);

            files.forEach((file) => {
                const filePath = resolve(targetDir, file);
                const fileExt = extname(file);

                if (fileExt === '.js') {
                    const addin = addins.find((a) => file.startsWith(a.name + '.js'));

                    if (addin) {
                        if (addin.kind === 'module') {
                            console.log(`跳过模块文件: ${file}`);
                            return;
                        }

                        const targetPath = resolve(addinsDir, addin.entry);
                        copyFile(filePath, targetPath, (copyErr) => {
                            if (copyErr) {
                                console.error(`拉取文件失败: ${file} -> ${targetPath}`, copyErr);
                            } else {
                                console.log(`拉取文件成功: ${file} -> ${targetPath}`);
                            }
                        });
                    } else {
                        const contentPath = resolve(contentDir, file);
                        copyFile(filePath, contentPath, (copyErr) => {
                            if (copyErr) {
                                console.error(`拉取新文件失败: ${file} -> ${contentPath}`, copyErr);
                            } else {
                                console.log(`添加新文件到内容目录: ${file} -> ${contentPath}`);
                            }
                        });
                    }
                }
            });
        } catch (parseErr) {
            console.error('解析 JSON 文件失败:', parseErr);
        }
    });
});