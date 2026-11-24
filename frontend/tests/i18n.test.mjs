import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "node:test";
import { fileURLToPath } from "node:url";
import { runInNewContext } from "node:vm";
import ts from "typescript";

// Use the existing compiler so these tests also run on the supported Node 20 runtime.
const filename = fileURLToPath(new URL("../src/lib/i18n.ts", import.meta.url));
const compiled = ts.transpileModule(readFileSync(filename, "utf8"), {
  compilerOptions: { module: ts.ModuleKind.CommonJS, target: ts.ScriptTarget.ES2022 },
}).outputText;
const context = { exports: {} };
runInNewContext(compiled, context, { filename });
const { createTranslator, greekMessages, resolveLocale, localizeMessage, formatTuition } = context.exports;

test("a missing or unsupported saved language resolves to Greek, independent of browser language", () => {
  for (const cookie of [undefined, "", "fr", "EN", "el-GR", "<script>"]) {
    assert.equal(resolveLocale(cookie), "el");
  }
  assert.equal(resolveLocale("el"), "el");
  assert.equal(resolveLocale("en"), "en");
});

test("every message has a nonempty Greek translation with exactly the same named placeholders", () => {
  const placeholders = value => [...value.matchAll(/\{(\w+)\}/g)].map(match => match[1]).sort();
  for (const [english, greek] of Object.entries(greekMessages)) {
    assert.ok(greek.trim(), english);
    assert.deepEqual(placeholders(greek), placeholders(english), english);
    const params = Object.fromEntries(placeholders(english).map(name => [name, `VALUE_${name}`]));
    for (const locale of ["el", "en"]) {
      const output = createTranslator(locale)(english, params);
      assert.doesNotMatch(output, /\{\w+\}/, `${locale}: ${english}`);
      for (const value of Object.values(params)) assert.ok(output.includes(value), english);
    }
  }
});

test("interpolation preserves supplied content verbatim and rejects omitted arguments", () => {
  const name = "School {city} & <Academy>";
  for (const locale of ["el", "en"]) {
    const t = createTranslator(locale);
    assert.ok(t("{name} in {city}, Greece.", { name, city: "Αθήνα" }).includes(name));
    assert.throws(() => t("{name} in {city}, Greece.", { name }), /Missing translation parameter: city/);
  }
});

test("known errors can change language without swallowing unknown server diagnostics", () => {
  const error = "Email or password is incorrect.";
  assert.notEqual(localizeMessage("el", error), error);
  assert.equal(localizeMessage("en", error), error);
  const diagnostic = "Unexpected API diagnostic {requestId}";
  assert.equal(localizeMessage("el", diagnostic), diagnostic);
  assert.equal(localizeMessage("el", "Hi, {name}"), "Hi, {name}");
});

test("tuition uses the selected language and currency conventions, including zero and missing fees", () => {
  const school = { isPrivate: true, tuitionFrom: 1234.5, tuitionTo: 2000 };
  assert.equal(formatTuition("en", school, true), "€1,234.5 – €2,000 per year");
  assert.equal(formatTuition("el", school, true), "1.234,5\u00a0€ – 2.000\u00a0€ ανά έτος");
  assert.equal(formatTuition("el", { ...school, tuitionFrom: 0, tuitionTo: null }), "0\u00a0€+ / έτος");
  assert.equal(formatTuition("el", { ...school, tuitionFrom: null }), "Δεν αναφέρονται δίδακτρα");
  assert.equal(formatTuition("en", { ...school, tuitionFrom: null }, true), "Contact school for tuition");
  assert.equal(formatTuition("en", { ...school, isPrivate: false }), "Public school");
});
