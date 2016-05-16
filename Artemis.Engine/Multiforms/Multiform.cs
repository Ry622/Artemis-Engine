﻿#region Using Statements

using Artemis.Engine.Utilities;
using Artemis.Engine.Utilities.UriTree;

using System.Collections.Generic;
using System.Linq;

#endregion

namespace Artemis.Engine.Multiforms
{

    public sealed class FormGroup : UriTreeMutableGroup<FormGroup, Form>
    {
        public FormGroup(string name) : base(name) { }
    }

    /// <summary>
    /// A Multiform represents a specific part of a game with a specific
    /// update loop and a specific render loop.
    /// </summary>
    public abstract class Multiform : ArtemisObject
    {

        private static AttributeMemoService<Multiform> attrMemoService
            = new AttributeMemoService<Multiform>();

        static Multiform()
        {
            attrMemoService.RegisterHandler<ReconstructMultiformAttribute>(m => { m.reconstructable = true; });
        }

        private const string TOP_FORM_GROUP_NAME = "ALL"; // The name of _allForms.
        private FormGroup _allForms; // The root FormGroup.
        private Renderer renderer; // The current renderer for the multiform.
        private bool reconstructable; // Whether or not the multiform uses reconstruction upon multiple activation.

        /// <summary>
        /// The name of the multiform instance.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The MultiformManager this multiform is registered to.
        /// </summary>
        public MultiformManager Manager { get; private set; }

        /// <summary>
        /// Whether or not this multiform has been registered to a multiform manager.
        /// </summary>
        public bool Registered { get { return Manager != null; } }

        /// <summary>
        /// The number of times this multiform has been activated.
        /// </summary>
        public int TimesActivated { get; private set; }

        /// <summary>
        /// The transition constraints on this multiform.
        /// </summary>
        public TransitionConstraintsAttribute TransitionConstraints { get; private set; }

        public Multiform() : this(null) { }

        public Multiform(string name)
        {
            if (name == null)
            {
                var type = GetType();
                Name = Reflection.HasAttribute<NamedMultiformAttribute>(type)
                    ? Reflection.GetFirstAttribute<NamedMultiformAttribute>(type).Name
                    : type.Name;
            }
            else
            {
                Name = name;
            }

            attrMemoService.Handle(this);

            _allForms = new FormGroup(TOP_FORM_GROUP_NAME);
        }

        private void HandleTransitionConstraints()
        {
            TransitionConstraints = Reflection.GetFirstAttribute<TransitionConstraintsAttribute>(GetType());
        }

        /// <summary>
        /// Called after the multiform is registered to a manager.
        /// </summary>
        /// <param name="manager"></param>
        internal void PostRegister(MultiformManager manager)
        {
            Manager = manager;
        }

        internal void InternalConstruct(MultiformConstructionArgs args)
        {
            TimesActivated++;
            if (reconstructable && TimesActivated > 1)
            {
                Reconstruct(args);
            }
            else
            {
                Construct(args);
            }
        }

        /// <summary>
        /// The main constructor for the multiform. This is called every time this multiform
        /// instance is switched to by the MultiformManager.
        /// </summary>
        public abstract void Construct(MultiformConstructionArgs args);

        /// <summary>
        /// The auxiliary constructor called every time after the first time the multiform is
        /// activated. This is only used if the multiform is decorated with a ReconstructMultiform
        /// attribute.
        /// </summary>
        public virtual void Reconstruct(MultiformConstructionArgs args) { }

        /// <summary>
        /// The deconstructor for the multiform. This is called when the multiform is deactivated (after
        /// Deactivate is called).
        /// </summary>
        public virtual void Deconstruct() { }

        /// <summary>
        /// Deactivate this multiform.
        /// </summary>
        public void Deactivate()
        {
            Manager.Deactivate(this);
        }

        /// <summary>
        /// Add a form to this multiform.
        /// </summary>
        /// <param name="form"></param>
        public void AddForm(Form form, bool disallowDuplicates = true)
        {
            if (form.Name == null)
            {
                _allForms.AddAnonymousItem(form);
            }
            else
            {
                _allForms.InsertItem(form.Name, form, disallowDuplicates);
            }
            form.Parent = this;
        }

        /// <summary>
        /// Add an anonymous form to the group with the given name.
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="form"></param>
        public void AddAnonymousForm(string groupName, Form form)
        {
            _allForms.InsertAnonymousItem(groupName, form);
        }

        /// <summary>
        /// Add the given forms to this multiform.
        /// </summary>
        /// <param name="disallowDuplicates"></param>
        /// <param name="forms"></param>
        public void AddForms(bool disallowDuplicates = true, params Form[] forms)
        {
            AddForms(disallowDuplicates, forms);
        }

        /// <summary>
        /// Add the given forms to this multiform.
        /// </summary>
        /// <param name="forms"></param>
        public void AddForms(IEnumerable<Form> forms, bool disallowDuplicates = true)
        {
            foreach (var form in forms)
                AddForm(form, disallowDuplicates);
        }

        /// <summary>
        /// Add the given forms anonymously to the group with the given name.
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="forms"></param>
        public void AddAnonymousForms(string groupName, params Form[] forms)
        {
            AddAnonymousForms(groupName, forms);
        }

        /// <summary>
        /// Add the given forms anonymously to the group with the given name.
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="forms"></param>
        public void AddAnonymousForms(string groupName, IEnumerable<Form> forms)
        {
            foreach (var form in forms)
            {
                AddAnonymousForm(groupName, form);
            }
        }

        /// <summary>
        /// Get the form with the given name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Form GetForm(string name)
        {
            return _allForms.GetItem(name);
        }

        /// <summary>
        /// Get the anonymous forms from the group with the given name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IEnumerable<Form> GetAnonymousForms(string name)
        {
            return _allForms.GetSubnode(name).AnonymousItems;
        }

        /// <summary>
        /// Get the forms with the given names.
        /// </summary>
        /// <param name="names"></param>
        /// <returns></returns>
        public IEnumerable<Form> GetForms(params string[] names)
        {
            return GetForms(names);
        }

        /// <summary>
        /// Get the forms with the given names.
        /// </summary>
        /// <param name="names"></param>
        /// <returns></returns>
        public IEnumerable<Form> GetForms(IEnumerable<string> names)
        {
            return from name in names select _allForms.GetItem(name);
        }

        /// <summary>
        /// Remove the form with the given name.
        /// </summary>
        /// <param name="name"></param>
        public void RemoveForm(string name)
        {
            _allForms.RemoveItem(name);
        }

        /// <summary>
        /// Remove the given form.
        /// </summary>
        /// <param name="form"></param>
        /// <param name="searchRecursive"></param>
        public void RemoveForm(Form form, bool searchRecursive = true)
        {
            if (form.Anonymous)
                RemoveAnonymousForm(form, searchRecursive);
            else
                RemoveForm(form.Name);
        }

        /// <summary>
        /// Remove the given anonymous form.
        /// </summary>
        /// <param name="form"></param>
        /// <param name="searchRecursive"></param>
        public void RemoveAnonymousForm(Form form, bool searchRecursive = true)
        {
            _allForms.RemoveAnonymousItem(form, searchRecursive);
        }

        /// <summary>
        /// Remove the anonymous form in the group with the given name.
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="form"></param>
        public void RemoveAnonymousForm(string groupName, Form form)
        {
            _allForms.RemoveAnonymousItem(groupName, form);
        }

        /// <summary>
        /// Remove the forms with the given names.
        /// </summary>
        /// <param name="names"></param>
        public void RemoveForms(params string[] names)
        {
            RemoveForms(names);
        }

        /// <summary>
        /// Remove the forms with the given names.
        /// </summary>
        /// <param name="names"></param>
        public void RemoveForms(IEnumerable<string> names)
        {
            foreach (var name in names)
                RemoveForm(name);
        }

        /// <summary>
        /// Remove the given forms.
        /// </summary>
        /// <param name="searchRecursive"></param>
        /// <param name="forms"></param>
        public void RemoveForms(bool searchRecursive = true, params Form[] forms)
        {
            RemoveForms(forms, searchRecursive);
        }

        /// <summary>
        /// Remove the given forms.
        /// </summary>
        /// <param name="forms"></param>
        /// <param name="searchRecursive"></param>
        public void RemoveForms(IEnumerable<Form> forms, bool searchRecursive = true)
        {
            foreach (var form in forms)
                RemoveForm(form, searchRecursive);
        }

        /// <summary>
        /// Remove the given anonymous forms.
        /// </summary>
        /// <param name="searchRecursive"></param>
        /// <param name="forms"></param>
        public void RemoveAnonymousForms(bool searchRecursive = true, params Form[] forms)
        {
            RemoveAnonymousForms(forms, searchRecursive);
        }

        /// <summary>
        /// Remove the given anonymous forms.
        /// </summary>
        /// <param name="forms"></param>
        /// <param name="searchRecursive"></param>
        public void RemoveAnonymousForms(IEnumerable<Form> forms, bool searchRecursive = true)
        {
            foreach (var form in forms)
                RemoveAnonymousForm(form, searchRecursive);
        }

        /// <summary>
        /// Remove all the forms.
        /// </summary>
        /// <param name="recursive"></param>
        public void ClearForms(bool recursive = false)
        {
            _allForms.ClearItems(recursive);
        }

        /// <summary>
        /// Remove all the forms in the given group.
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="recursive"></param>
        public void ClearForms(string groupName, bool recursive = false)
        {
            _allForms.GetSubnode(groupName).ClearItems(recursive);
        }

        /// <summary>
        /// Remove all the named forms (leaving only the anonymous ones).
        /// </summary>
        /// <param name="recursive"></param>
        public void ClearNamedForms(bool recursive = false)
        {
            _allForms.ClearNamedItems(recursive);
        }

        /// <summary>
        /// Remove all the named forms that match the given regex.
        /// </summary>
        /// <param name="regex"></param>
        /// <param name="recursive"></param>
        public void ClearNamedForms(string regex, bool recursive = false)
        {
            _allForms.ClearNamedItems(regex, recursive);
        }

        /// <summary>
        /// Remove all the named forms in the given group that match the given regex.
        /// 
        /// NOTE: The `regex` parameter can be null.
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="regex"></param>
        /// <param name="recursive"></param>
        public void ClearNamedForms(string groupName, string regex, bool recursive = false)
        {
            var subnode = _allForms.GetSubnode(groupName);
            if (regex == null)
                subnode.ClearNamedItems(recursive);
            else
                subnode.ClearNamedItems(regex, recursive);
        }

        /// <summary>
        /// Remove all the anonymous forms (leaving only the named ones).
        /// </summary>
        /// <param name="recursive"></param>
        public void ClearAnonymousForms(bool recursive = false)
        {
            _allForms.ClearAnonymousItems(recursive);
        }

        /// <summary>
        /// Remove all the anonymous forms from the group with the given name (leaving only the named ones).
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="recursive"></param>
        public void ClearAnonymousForms(string groupName, bool recursive = false)
        {
            _allForms.GetSubnode(groupName).ClearAnonymousItems(recursive);
        }

        /// <summary>
        /// Set the current renderer for this multiform.
        /// </summary>
        /// <param name="action"></param>
        protected void SetRenderer(Renderer action)
        {
            renderer = action;
        }

        internal void Render()
        {
            renderer();
        }
    }
}
