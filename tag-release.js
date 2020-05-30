const
  promisify = require("util").promisify,
  mkdirp = require("mkdirp").sync,
  path = require("path"),
  exec = promisify(require("child_process").exec);

async function createZip(version, baseFolder, runtime, append) {
  let src = path.join(baseFolder, runtime, "publish", "bitsplat");
  if (append) {
    src += append;
  }
  const zip = `bitsplat-${runtime}-${version}.zip`;
  console.log(`create release zip: ${zip}`);
  await exec(`zip -j -9 release/${zip} "${src}"`);
}

class Version {
  constructor(tag) {
    const parts = tag.replace(/^v/, "").split(".");
    this.tag = tag;
    this.major = parseInt(parts[0]);
    this.minor = parseInt(parts[1] || 0);
    this.patch = parseInt(parts[2] || 0);
  }

  compare(otherVersion) {
    return ["major", "minor", "patch"].reduce(
      (acc, cur) => acc || this._comparePart(cur, otherVersion),
      0
    );
  }

  _comparePart(partName, otherVersion) {
    return this[partName] - otherVersion[partName];
  }
}

(async function () {
  const
    execResult = await exec("git tag"),
    lines = execResult.stdout.trim().split("\n").map(l => l.trim()).sort(),
    versions = lines.map(line => new Version(line)),
    latestVersion = versions.sort((a, b) => b.compare(a))[0],
    major = latestVersion.major,
    minor = latestVersion.minor,
    next = `${major}.${minor + 1}`,
    cmd1 = `git add -A :/`,
    cmd2 = `git commit -m ":bookmark: release v${next}"`,
    cmd3 = `git tag v${next}`;
  mkdirp("release");
  try {
    // TODO: should update the .csproj version
    // TODO: switch to simple-git instead of running commands
    if (!process.env.SKIP_COMMIT) {
      console.log(`commit release v${next}`);
      await exec(cmd1);
      await exec(cmd2);
    }
    console.log(`tagging at v${next}`);
    await exec(cmd3);
    console.log(`pushing...`);
    await exec("git push");
    await exec("git push --tags");

    const baseFolder = path.join("src", "bitsplat", "bin", "Release", "netcoreapp3.1");
    await createZip(next, baseFolder, "linux-x64");
    await createZip(next, baseFolder, "osx-x64");
    await createZip(next, baseFolder, "win-x64", ".exe");
  } catch (e) {
    console.error(e);
    process.exit(1)
  }
})();
