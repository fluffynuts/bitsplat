const 
    promisify = require("util").promisify,
    path = require("path"),
    exec = promisify(require("child_process").exec);

    async function createZip(version, baseFolder, runtime, append) {
        let src = path.join(baseFolder, runtime, publish, "bitsplat");
        if (append) {
            src += append;
        }
        await exec(`zip -9 releases/bitsplat-${runtime}-${version}.zip "${src}"`);
    }

(async function() {
    const 
        execResult = await exec("git tag | tail -n 1"),
        latestTag = execResult.stdout.trim(),
        latestTagVersion = latestTag.replace(/[^0-9.]*/g, ""),
        parts = latestTagVersion.split("."),
        major = parseInt(parts[0]),
        minor = parseInt(parts[1]),
        next = `${major}.${minor + 1}`,
        cmd1 = `git add -A :/`,
        cmd2 = `git commit -m ":bookmark: release v${next}"`,
        cmd3 = `git tag v${next}`;
    console.log(`commit release v${next}`);
    await exec(cmd1);
    await exec(cmd2);
    console.log(`tagging at v${next}`);
    await exec(cmd3);
    console.log(`pushing...`);
    await exec("git push");
    await exec("git push --tags");

    const baseFolder = path.join("src", "bitsplat", "bin", "Release", "netcoreapp3.1");
    await createZip(next, baseFolder, "linux-64");
    await createZip(next, baseFolder, "osx-64");
    await createZip(next, baseFolder, "win-x64", ".exe");
})();
