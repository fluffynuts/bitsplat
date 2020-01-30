const 
    promisify = require("util").promisify,
    exec = promisify(require("child_process").exec);

(async function() {
    const 
        execResult = await exec("git tag | head -n 1"),
        latestTag = execResult.stdout.trim(),
        latestTagVersion = latestTag.replace(/[^0-9.]*/g, ""),
        asFloat = parseFloat(latestTagVersion),
        next = asFloat + 0.1,
        cmd1 = `git add -A :/`,
        cmd2 = `git commit -m ":bookmark: release v${next}"`,
        cmd3 = `git tag v${next}`;
    console.log(`commit release v${next}`);
    await exec(cmd1);
    await exec(cmd2);
    console.log(`tagging at v${next}`);
    await exec(cmd3);
    console.log(`pushing...`);
    await exec("git push --tags");
})();
