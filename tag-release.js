const 
    promisify = require("util").promisify,
    exec = promisify(require("child_process").exec);

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
    await exec("git push --follow-tags");
})();
