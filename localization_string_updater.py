import re
import os
import json
import argparse


def loc_finder(path):
    localization_strings = {}
    for root, dirs, files in os.walk(path):
        path = root.split(os.sep)
        for code_file in files:
            if code_file.endswith(".cs"):
                with open(os.sep.join(path+[code_file]), 'r', encoding='utf-8') as f:
                    for line in f.readlines():
                        x = re.findall('''"\$chebgonaz_[A-Za-z_]+"''', line)
                        if x and len(x) > 0:
                            localization_strings[x[0].replace('"', '')] = ''
    return localization_strings


def translation_file_finder(path):
    en = None
    file_paths = []
    for root, dirs, files in os.walk(path):
        path = root.split(os.sep)
        for translation_file in files:
            if translation_file.endswith(".json"):
                if 'english' in translation_file:
                    en = os.sep.join(path + [translation_file])
                else:
                    file_paths.append(os.sep.join(path + [translation_file]))
    return [en] + file_paths


if __name__ == '__main__':
    parser = argparse.ArgumentParser(prog='Localization Updater', description='Updates localization strings in files')
    parser.add_argument('mod_path', nargs='?', default="ChebsMercenaries", type=str)
    parser.add_argument('translations_path', nargs='?', default="Translations", type=str)
    args = parser.parse_args()

    mod_path = args.mod_path
    translations_path = args.translations_path

    locs = loc_finder(mod_path)
    files = translation_file_finder(translations_path)
    english = None
    for file in files:
        print(f'Opening {file}...')
        with open(file, 'r', encoding='utf-8') as f:
            file_contents = json.load(f)
        os.remove(file)
        with open(file, 'w', encoding='utf-8') as f:
            print(f'Merging...')
            merged_contents = {**locs, **file_contents}
            for key in merged_contents.keys():
                if merged_contents[key] == '' and english is not None:
                    merged_contents[key] = english[key]
            print('Writing...')
            json.dump(merged_contents, f, indent=4, ensure_ascii=False)
            if english is None:
                english = merged_contents
